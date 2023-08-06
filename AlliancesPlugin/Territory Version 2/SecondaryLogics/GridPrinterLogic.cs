using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public Vector3 CentrePosition { get; set; }
        private List<IMyProjector> FoundProjectors = new List<IMyProjector>();
        private Dictionary<long, DateTime> Cooldowns = new Dictionary<long, DateTime>();
        public int BlocksPerCycle = 5;
        public int MaximumProjectors = 1;
        public int SecondsBetweenLoops { get; set; } = 10;

        public Task<bool> DoSecondaryLogic(ICapLogic point, Territory territory)
        {
            if (!Enabled)
            {
                return Task.FromResult(true);
            }

            if (!CanLoop()) return Task.FromResult(true);

            NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);

            IPointOwner temp = point.PointOwner ?? territory.Owner;

            if (RequireOwner && temp == null)
            {
                return Task.FromResult(true);
            }

            FindProjectors();
            foreach (var grid in FoundProjectors)
            {

            }
            throw new NotImplementedException();
        }

        public void FindProjectors()
        {
            FoundProjectors.Clear();
            int found = 0;
            var sphere = new BoundingSphereD(CentrePosition, 500 * 2);
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<IMyProjector>())
            {

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
