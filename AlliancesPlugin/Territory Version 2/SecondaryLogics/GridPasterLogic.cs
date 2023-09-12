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
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace AlliancesPlugin.Territory_Version_2.SecondaryLogics
{
    public class GridPasterLogic : ISecondaryLogic
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
        public bool RequireMoney = true;
        public bool RequireComponents = false;
        public string GridOwnerTag = "CIA";
        public string CargoGridTerminalName = "Shipyard Cargo";
        public bool UseNamedCargo = true;

        private List<MyCubeGrid> FoundGrids = new List<MyCubeGrid>();
        private List<IMyProjector> GridsProjectors = new List<IMyProjector>();

        private List<IMyInventory> FoundInventories = new List<IMyInventory>();
        private bool setup = false;
        public Task<bool> DoSecondaryLogic(ICapLogic point, Territory territory)
        {

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

            try
            {
                foreach (var prooo in GridsProjectors)
                {
                    
                }
            }
            catch (Exception e)
            {
                AlliancePlugin.Log.Error($"Grid paster error {e}");
                return Task.FromResult(true);
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
                if ((fac != null && !fac.Tag.Equals(GridOwnerTag)) || fac == null)
                {
                    continue;
                }

                if (fac.Tag != GridOwnerTag) continue;

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
                //         AlliancePlugin.Log.Error("A");
                var fac = MySession.Static.Factions.GetPlayerFaction(FacUtils.GetOwner((MyCubeGrid)grid.CubeGrid));
                if (fac != null && fac.Tag == GridOwnerTag)
                {
                    //       AlliancePlugin.Log.Error("C");
                    GridsProjectors.Add(grid);
                    continue;
                }
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
                    //       AlliancePlugin.Log.Error("B");
                    FoundProjectors.Add(grid);
                }
            }
        }

        public DateTime NextLoop { get; set; }

        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }

        public int Priority { get; set; } = 1;
    }
}
