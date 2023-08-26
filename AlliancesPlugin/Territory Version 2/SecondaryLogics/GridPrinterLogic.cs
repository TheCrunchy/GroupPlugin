using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances.Upgrades;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using AlliancesPlugin.Territory_Version_2.Models;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
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
        public int MaximumProjectors = 1;
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
                    //find the pilot and charge them instead of the grid owner 
                    GridRepair.BuildProjected(grid.CubeGrid as MyCubeGrid, FacUtils.GetOwner(grid.CubeGrid as MyCubeGrid), 10, 10, PrinterPrefix, PriceMultiplier);
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
            var sphere = new BoundingSphereD(CentrePosition, 500 * 2);

            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<IMyProjector>())
            {
                if (Cooldowns.TryGetValue(grid.EntityId, out var time))
                {
                    if (DateTime.Now < time)
                    {
                        continue;
                    }
                }
                if (FoundProjectors.Count < MaximumProjectors)
                {
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
