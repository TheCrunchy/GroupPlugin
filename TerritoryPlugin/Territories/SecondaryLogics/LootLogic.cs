using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Territory.Territories.Interfaces;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders.Private;
using VRageMath;
using IMyInventory = VRage.Game.ModAPI.IMyInventory;

namespace Territory.Territories.SecondaryLogics
{
    public class LootLogic : ISecondaryLogic
    {
        public class LootItem
        {
            public string TypeId = "Ore";
            public string SubtypeId = "Iron";
            public double MinAmount = 1;
            public double MaxAmount = 5;
        }

        public DateTime NextLoop { get; set; }
        public Vector3 GridPosition { get; set; }
        public string GridOwnerFacTag = "SPRT";
        public bool SpawnInNamedCargo = false;
        public string NamedCargoTerminalName = "Default";
        public int SecondsBetweenLoops { get; set; } = 3600;
        public bool RequireOwner = true;
        public bool MultiplyLootAmountByOwnershipPercentage = true;

        public List<LootItem> Loot = new List<LootItem>();
        private List<MyCubeGrid> FoundGrids = new List<MyCubeGrid>();

        public bool Enabled { get; set; }

        public Task<bool> DoSecondaryLogic(ICapLogic point, Models.Territory territory)
        {
            if (!Enabled)
            {
                return Task.FromResult(true);
            }
            if (!CanLoop()) return Task.FromResult(true);
            NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);

            var temp = point.PointOwner ?? territory.Owner;

            if (RequireOwner && point.PointOwner == null)
            {
                return Task.FromResult(true);
            }
            FindGrids();
            var inventory = GetGridInventory();
            if (inventory == null)
            {
                TerritoryPlugin.Log.Info($"Could not find inventory for grid at position {GridPosition.ToString()}");
                return Task.FromResult(true);
            }

            foreach (var item in Loot.Where(item => !string.IsNullOrEmpty(item.TypeId)))
            {
                if (!MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubtypeId, out var id))
                    continue;
                    
                var amount = TerritoryPlugin.random.NextDouble() * (item.MaxAmount - item.MinAmount) + item.MaxAmount;
                if (MultiplyLootAmountByOwnershipPercentage)
                {
                    amount *= territory.PercentOwned;
                }
                SpawnItems(id, (MyFixedPoint)amount);
            }
            return Task.FromResult(true);
        }
        public void FindGrids()
        {
            FoundGrids.Clear();
            var sphere = new BoundingSphereD(GridPosition, 2500 * 2);
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
            {
                var fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                if ((fac != null && !fac.Tag.Equals(GridOwnerFacTag)) || fac == null)
                {
                    continue;
                }

                if (fac.Tag != GridOwnerFacTag) continue;
                FoundGrids.Add(grid);
            }
        }

        public bool SpawnItems(MyDefinitionId id, MyFixedPoint amount)
        {
            foreach (var cargo in GetGridInventory())
            {
                MyItemType itemType = new MyInventoryItemFilter(id.TypeId + "/" + id.SubtypeName).ItemType;
                if (cargo.CanItemsBeAdded(amount, itemType))
                {
                    cargo.AddItems(amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializerKeen.CreateNewObject(id));
                    return true;
                }
            }

            return false;
        }
        public List<IMyInventory> GetGridInventory()
        {
            var foundInvents = new List<IMyInventory>();
            foreach (var grid in FoundGrids)
            {
                if (SpawnInNamedCargo)
                {
                    var gridTerminalSys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

                    //     var block = gridTerminalSys.GetBlocks()
                    var blocks = new List<Sandbox.ModAPI.IMyTerminalBlock>();
                    gridTerminalSys.GetBlocks(blocks);
                    foreach (var block in blocks.Where(x => x.CustomName.Trim() == this.NamedCargoTerminalName))
                    {
                        foundInvents.Add(block.GetInventory());
                    }
                }
                else
                {
                    var cargo = grid.GetFatBlocks().OfType<MyCargoContainer>();
                    foreach (var carg in cargo)
                    {
                        foundInvents.Add(carg.GetInventory());
                    }
                }
            }

            return foundInvents;
        }

        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }

        public int Priority { get; set; } = 1;
    }
}
