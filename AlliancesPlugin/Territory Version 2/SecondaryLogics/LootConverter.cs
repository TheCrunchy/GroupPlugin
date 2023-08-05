using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances.NewTerritories;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using AlliancesPlugin.Territory_Version_2.Models;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;
using VRageMath;
using IMyInventory = VRage.Game.ModAPI.IMyInventory;
using IMyTextPanel = Sandbox.ModAPI.IMyTextPanel;

namespace AlliancesPlugin.Territory_Version_2.SecondaryLogics
{
    public class LootConverter : ISecondaryLogic
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
        public string NamedCargoTerminalName = "[CRAFT CARGO]";
        public string NamedLCD = "[CRAFT OUTPUT]";
        public int SecondsBetweenLoops { get; set; } = 3600;
        public bool RequireOwner = true;

        public List<CraftedItem> CraftableItems = new List<CraftedItem>();
        
        private List<MyCubeGrid> FoundGrids = new List<MyCubeGrid>();

        public bool Enabled { get; set; }
        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }

        public int Priority { get; set; }
        public Task<bool> DoSecondaryLogic(ICapLogic point, Territory territory)
        {
            if (!Enabled)
            {
                return Task.FromResult(true);
            }

            if (!CanLoop()) return Task.FromResult(true);

            NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
            if (RequireOwner && point.PointOwner == null)
            {
                return Task.FromResult(true);
            }
            FindGrids();
            var inventory = GetGridInventory();
            if (inventory == null || !FoundGrids.Any())
            {
                AlliancePlugin.Log.Info($"Could not find inventory for grid at position {GridPosition.ToString()}");
                return Task.FromResult(true);
            }
            var LCD = GetPanel();

            StringBuilder builder = new StringBuilder();
            var inventories = GetGridInventory();
            foreach (CraftedItem item in this.CraftableItems.Where(x => x.Enabled))
            {
                double yeet = AlliancePlugin.rand.NextDouble();
                if (!(yeet <= item.chanceToCraft)) continue;
                var comps = new Dictionary<MyDefinitionId, int>();
   
                if (!MyDefinitionId.TryParse("MyObjectBuilder_" + item.typeid, item.subtypeid,
                        out MyDefinitionId id)) continue;
                foreach (RecipeItem recipe in item.RequriedItems)
                {
                    if (MyDefinitionId.TryParse("MyObjectBuilder_" + recipe.typeid, recipe.subtypeid, out MyDefinitionId id2))
                    {
                        comps.Add(id2, recipe.amount);
                    }
                }

   
                if (!ConsumeComponents(inventory, comps, LCD)) continue;
                SpawnItems(id, item.amountPerCraft);
                builder.AppendLine($"Successfully crafted {item.subtypeid} {item.typeid.Replace("MyObjectBuilder_", "")}");
                comps.Clear();
            }

            LCD?.WriteText(builder.ToString());
            return Task.FromResult(true);
        }

        public void FindGrids()
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
                    cargo.AddItems(amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
                    return true;
                }
            }

            return false;
        }
        public static bool ConsumeComponents(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, IDictionary<MyDefinitionId, int> components, IMyTextPanel panel)
        {
            List<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, VRage.MyFixedPoint>> toRemove = new List<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, VRage.MyFixedPoint>>();
            foreach (KeyValuePair<MyDefinitionId, int> c in components)
            {
                MyFixedPoint needed = CountComponentsTwo(inventories, c.Key, c.Value, toRemove);
                if (needed > 0)
                {
                    panel?.WriteText("Crafting error, missing " + needed + " " + c.Key.SubtypeName + " All components must be inside this grid.");
                }
                return false;
            }

            foreach (MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint> item in toRemove)
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    item.Item1.RemoveItemAmount(item.Item2, item.Item3);
                });
            return true;
        }

        public IMyTextPanel GetPanel()
        {
            try
            {
                foreach (var grid in FoundGrids)
                {
                    var gridTerminalSys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

                    var block = gridTerminalSys.GetBlockWithName(NamedLCD);

                    return (IMyTextPanel)block;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static MyFixedPoint CountComponentsTwo(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, MyDefinitionId id, int amount, ICollection<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>> items)
        {
            MyFixedPoint targetAmount = amount;
            foreach (var inv in inventories)
            {
                var invItem = inv.FindItem(id);
                if (invItem == null) continue;
                if (invItem.Amount >= targetAmount)
                {
                    items.Add(new MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>(inv, invItem, targetAmount));
                    targetAmount = 0;
                    break;
                }

                items.Add(new MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>(inv, invItem, invItem.Amount));
                targetAmount -= invItem.Amount;
            }
            return targetAmount;
        }


        public List<IMyInventory> GetGridInventory()
        {
            var foundInvents = new List<IMyInventory>();
            foreach (var grid in FoundGrids)
            {
                if (SpawnInNamedCargo)
                {
                    var gridTerminalSys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

                    var block = gridTerminalSys.GetBlockWithName(NamedCargoTerminalName);
                    if (block != null)
                    {
                        foundInvents.Add(block.GetInventory());
                    }
                }
                else
                {
                    var cargo = grid.GetBlocks().OfType<MyCargoContainer>();
                    foreach (var carg in cargo)
                    {
                        foundInvents.Add(carg.GetInventory());
                    }
                }
            }

            return null;
        }

    }
}
