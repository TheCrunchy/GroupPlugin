using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances.NewTerritories;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using NLog.Fluent;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace AlliancesPlugin.Territory_Version_2.SecondaryLogics
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

        public bool Enabled { get; set; }

        public Task DoSecondaryLogic(ICapLogic point, Territory territory)
        {
            if (!Enabled)
            {
                return Task.CompletedTask;
            }
            if (!CanLoop()) return Task.CompletedTask;
            NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
            if (RequireOwner && point.PointOwner == null)
            {
                return Task.CompletedTask;
            }

            var inventory = GetGridInventory();
            if (inventory == null)
            {
                AlliancePlugin.Log.Info($"Could not find inventory for grid at position {GridPosition.ToString()}");
                return Task.CompletedTask;
            }

            foreach (var item in Loot.Where(item => !string.IsNullOrEmpty(item.TypeId)))
            {
                if (!MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubtypeId, out var id))
                    continue;
                    
                var amount = AlliancePlugin.random.NextDouble() * (item.MaxAmount - item.MinAmount) + item.MaxAmount;
                if (MultiplyLootAmountByOwnershipPercentage)
                {
                    amount *= territory.PercentOwned;
                }
                inventory.AddItems((MyFixedPoint)amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
            }
            return Task.CompletedTask;
        }

        public IMyInventory GetGridInventory()
        {
            var sphere = new BoundingSphereD(GridPosition, 2500 * 2);
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
            {
                var fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                if ((fac != null && !fac.Tag.Equals(GridOwnerFacTag)) || fac == null)
                {
                    continue;
                }

                if (fac.Tag != GridOwnerFacTag) continue;
                if (SpawnInNamedCargo)
                {
                    var gridTerminalSys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

                    var block = gridTerminalSys.GetBlockWithName(NamedCargoTerminalName);
                    if (block != null)
                    {
                        return block.GetInventory();
                    }
                }
                else
                {
                    var cargo = grid.GetBlocks().OfType<MyCargoContainer>();
                    if (cargo != null && cargo.Any())
                    {
                        return cargo.First().GetInventory();
                    }
                }
            }

            return null;
        }

        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }

    }
}
