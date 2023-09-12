using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using AlliancesPlugin.Territory_Version_2.Models;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace AlliancesPlugin.Territory_Version_2.SecondaryLogics
{
    public class UpkeepLogic : ISecondaryLogic
    {
        public bool Enabled { get; set; }
        public bool IsFueled = true;

        private List<MyCubeGrid> FoundGrids = new List<MyCubeGrid>();
        public string NamedLCD = "[UPKEEP OUTPUT]";

        public List<UpkeepItem> UpkeepItems = new List<UpkeepItem>();
        public Task<bool> DoSecondaryLogic(ICapLogic point, Territory territory)
        {
            if (!Enabled)
            {
                return Task.FromResult(true);
            }

            if (!CanLoop()) return Task.FromResult(IsFueled);
            NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);

            FindGrids();
            var inventory = GetGridInventory();
            var LCD = GetPanel();
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                LCD?.WriteText("");
            });
            if (inventory == null || !FoundGrids.Any() || !inventory.Any())
            {
                AlliancePlugin.Log.Info($"Could not find inventory for grid at position {GridPosition.ToString()}");
                // AlliancePlugin.Log.Info(inventory.Count);
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    LCD?.WriteText("Could not find inventory for grid!", true);
                });
                return Task.FromResult(false);
            }

            var comps = new Dictionary<MyDefinitionId, int>();

            foreach (var item in UpkeepItems)
            {
                if (!MyDefinitionId.TryParse("MyObjectBuilder_" + item.typeid, item.subtypeid,
                        out MyDefinitionId id)) return Task.FromResult(false);

                comps.Add(id, item.amount);
            }


            var result = ConsumeComponents(inventory, comps, LCD);

            IsFueled = result;
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
{
    LCD?.WriteText($"Upkeep status {(IsFueled ? "Facility is operating" : $"Facility is not operating, upkeep required.")}", true);
});

            return Task.FromResult(IsFueled);
        }
        public Vector3 GridPosition { get; set; }
        public string GridOwnerFacTag = "SPRT";
        public DateTime NextLoop { get; set; }
        public int SecondsBetweenLoops { get; set; } = 60;
        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
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
        public List<IMyInventory> GetGridInventory()
        {
            var foundInvents = new List<IMyInventory>();
            foreach (var grid in FoundGrids)
            {

                var cargo = grid.GetFatBlocks().OfType<MyCargoContainer>();
                foreach (var carg in cargo)
                {
                    foundInvents.Add(carg.GetInventory());
                }

            }

            return foundInvents;
        }
        public static bool ConsumeComponents(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, IDictionary<MyDefinitionId, int> components, IMyTextPanel panel)
        {
            List<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, VRage.MyFixedPoint>> toRemove = new List<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, VRage.MyFixedPoint>>();
            foreach (KeyValuePair<MyDefinitionId, int> c in components)
            {
                MyFixedPoint needed = CountComponentsTwo(inventories, c.Key, c.Value, toRemove);

                if (needed > 0)
                {
                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                    {
                        panel?.WriteText($"Upkeep, missing " + needed + " " + c.Key.SubtypeName + $" Item must be inside this grid. at {DateTime.Now:HH-mm}", true);
                    });
                    //   AlliancePlugin.Log.Info("Not found components");
                    return false;
                }
            }
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                foreach (MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint> item in toRemove)
                    item.Item1.RemoveItemAmount(item.Item2, item.Item3);
            });
            return true;
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

        public int Priority { get; set; } = 0;
    }
}
