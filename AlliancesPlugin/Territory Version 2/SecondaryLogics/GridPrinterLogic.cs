using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances.Upgrades;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using AlliancesPlugin.Territory_Version_2.Models;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Mod;
using Torch.Mod.Messages;
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

            FindProjectors();
            foreach (var grid in FoundProjectors)
            {
                try
                {
                    var grid2 = grid.CubeGrid as MyCubeGrid;
                //    AlliancePlugin.Log.Info("1");
                    foreach (MyCockpit cockpit in grid2.GetFatBlocks().OfType<MyCockpit>())
                    {
                        if (cockpit.Pilot != null)
                        {
                            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                                {
                                    GridRepair.BuildProjected(grid2,
                                        cockpit.Pilot.GetIdentity().IdentityId, RepairsPerCycle, BlocksPerCycle,
                                        PrinterPrefix, PriceMultiplier);
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
                    }
                    //find the pilot and charge them instead of the grid owner 
            
                }
                catch (Exception e)
                {
                    AlliancePlugin.Log.Error($"Grid printer error {e}");
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(true);
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
