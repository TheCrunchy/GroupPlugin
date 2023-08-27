using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances.Upgrades;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using AlliancesPlugin.Territory_Version_2.Models;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace AlliancesPlugin.Territory_Version_2.SecondaryLogics
{
    public class GridPrinterLogic : ISecondaryLogic
    {
        public bool Enabled { get; set; }
        public bool RequireOwner { get; set; }
        public string PrinterPrefix = "Welding Interns";
        //public bool ConsumeComponents { get; set; }
        //public string CargoGridFactionTag { get; set; }
        //public string CargoName { get; set; }
        public Vector3 CentrePosition { get; set; }

        private List<IMyProjector> FoundProjectors = new List<IMyProjector>();
        private Dictionary<long, DateTime> Cooldowns = new Dictionary<long, DateTime>();
        public int BlocksPerCycle = 5;
        public int RepairsPerCycle = 5;
        public int MaximumProjectors = 1;
        public int Radius = 250;
        public int SecondsBetweenLoops { get; set; } = 10;
        public float PriceMultiplier = 1f;
        public int PriceIfNotDefined = 1000;
        public bool RequireMoney = true;
        public bool RequireComponents = false;
        public string CargoGridOwnerTag = "CIA";
        public string CargoGridTerminalName = "Shipyard Cargo";
        public bool UseNamedCargo = true;
        private Dictionary<String, ComponentCost> ComponentCosts = new Dictionary<string, ComponentCost>();
        public List<ComponentCost> repairCost = new List<ComponentCost>();
        private List<MyCubeGrid> FoundGrids = new List<MyCubeGrid>();

        public void AddComponentCost(string subtype, long cost, bool banned)
        {
            if (repairCost.Any(x => x.SubTypeId == subtype))
            {
                return;
            }
            repairCost.Add(new ComponentCost()
            {
                SubTypeId = subtype,
                Cost = cost,
                IsBannedComponent = banned
            });
        }
        public void AddComponentCostToDictionary()
        {
            foreach (ComponentCost comp in repairCost)
            {
                if (!ComponentCosts.ContainsKey(comp.SubTypeId))
                {
                    ComponentCosts.Add(comp.SubTypeId, comp);
                }
            }
        }

        private List<IMyInventory> FoundInventories = new List<IMyInventory>();
        private bool setup = false;
        public Task<bool> DoSecondaryLogic(ICapLogic point, Territory territory)
        {

            if (!setup)
            {
                AddComponentCostToDictionary();
                foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
                {
                    if ((def as MyComponentDefinition) == null) continue;
                    var min = (def as MyComponentDefinition).MinimalPricePerUnit;
                    this.AddComponentCost(def.Id.SubtypeName, PriceIfNotDefined, false);
                }
                setup = true;
            }
            if (!Enabled)
            {
                return Task.FromResult(true);
            }

            if (!CanLoop()) return Task.FromResult(true);

            NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);

            IPointOwner temp = point.PointOwner ?? territory.Owner;

            if (RequireOwner && point.PointOwner == null)
            {
                return Task.FromResult(true);
            }

            if (RequireComponents)
            {
                FindGrids();
                var inventory = GetGridInventory();
                if (inventory == null || !FoundGrids.Any() || !inventory.Any())
                {
                    AlliancePlugin.Log.Info($"Could not find inventory for grid at position {CentrePosition.ToString()}");
                    // AlliancePlugin.Log.Info(inventory.Count);
                    return Task.FromResult(true);
                }
                FoundInventories = inventory;
            }

            FindProjectors();
            foreach (var grid in FoundProjectors)
            {
                try
                {
                    var grid2 = grid.CubeGrid as MyCubeGrid;
                    //    AlliancePlugin.Log.Info("1");

                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                        {
                            GridRepair.BuildProjected(grid2, grid.OwnerId, RepairsPerCycle, BlocksPerCycle,
                                PrinterPrefix, PriceMultiplier, ComponentCosts, RequireMoney, RequireComponents, FoundInventories);
                        }
                    );
                    if (Cooldowns.ContainsKey(grid.EntityId))
                    {
                        Cooldowns[grid.EntityId] = DateTime.Now.AddSeconds(SecondsBetweenLoops);
                    }
                    else
                    {
                        Cooldowns.Add(grid.EntityId, DateTime.Now.AddSeconds(SecondsBetweenLoops));
                    }

                    break;
                }
                catch (Exception e)
                {
                    AlliancePlugin.Log.Error($"Grid printer error {e}");
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(true);
        }

        public List<IMyInventory> GetGridInventory()
        {
            var foundInvents = new List<IMyInventory>();
            foreach (var grid in FoundGrids)
            {
                if (UseNamedCargo)
                {
                    var gridTerminalSys = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

                    //     var block = gridTerminalSys.GetBlocks()
                    var blocks = new List<Sandbox.ModAPI.IMyTerminalBlock>();
                    gridTerminalSys.GetBlocks(blocks);
                    foreach (var block in blocks.Where(x => x.CustomName.Trim() == this.CargoGridTerminalName))
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
        public void FindGrids()
        {
            FoundGrids.Clear();
            var sphere = new BoundingSphereD(CentrePosition, Radius * 2);
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
            {
                var fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                if ((fac != null && !fac.Tag.Equals(CargoGridOwnerTag)) || fac == null)
                {
                    continue;
                }

                if (fac.Tag != CargoGridOwnerTag) continue;
                FoundGrids.Add(grid);
            }
        }
        public void FindProjectors()
        {
            FoundProjectors.Clear();
            int found = 0;
            var sphere = new BoundingSphereD(CentrePosition, Radius * 2);

            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<IMyProjector>())
            {
                if (grid.ProjectedGrid == null)
                {
                    continue;
                }
                if (Cooldowns.TryGetValue(grid.EntityId, out var time))
                {
                    if (DateTime.Now < time)
                    {
                        continue;
                    }
                }
                if (FoundProjectors.Count < MaximumProjectors)
                {
                    //      AlliancePlugin.Log.Info("found");
                    FoundProjectors.Add(grid);
                }
            }
        }

        public DateTime NextLoop { get; set; }

        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }

        public int Priority { get; set; }
    }
}
