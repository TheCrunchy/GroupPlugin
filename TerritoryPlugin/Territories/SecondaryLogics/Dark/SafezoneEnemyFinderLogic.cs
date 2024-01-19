using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup.Territories.Interfaces;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game;
using VRageMath;
using VRageRender.Messages;

namespace CrunchGroup.Territories.SecondaryLogics.Dark
{
    public class SafezoneEnemyFinderLogic : ISecondaryLogic
    {
        public bool Enabled { get; set; }
        public Vector3 SafezonePosition { get; set; }

        public bool HasBeenSieged { get; set; }
        public DateTime SafezoneUpAtThisTime { get; set; }
        public DateTime SafezoneDownAtThisTime { get; set; }

        public DateTime AttackerLastFound { get; set; }

        public Task<bool> DoSecondaryLogic(ICapLogic point, Models.Territory territory)
        {
            if (!Enabled)
            {
                return Task.FromResult(true);
            }

            if (!CanLoop()) return Task.FromResult(true);

            NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);

            IPointOwner temp = point.PointOwner ?? territory.Owner;
            BoundingSphereD sphere = new BoundingSphereD(SafezonePosition, 10000);
        
            if (temp == null)
            {
                if (DebugMessages)
                {
                    Core.Log.Info($"Safezone Debug {point.PointName} owner is null");
                }
                return Task.FromResult(false);
            }

            var faction = temp.GetOwner() as MyFaction;
            if (faction == null)
            {
                if (DebugMessages)
                {
                    Core.Log.Info($"Safezone Debug {point.PointName} faction is null");
                }
                return Task.FromResult(false);
            }

            //check if defenders are online 

            if (DebugMessages)
            {
                Core.Log.Info($"Safezone Debug {point.PointName} zone logic cycle done");
            }
            
            if (HasBeenSieged)
            {
                //delete the zone 
                if (SafezoneDownAtThisTime <= DateTime.Now)
                {
                    foreach (MySafeZone zone in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MySafeZone>())
                    {
                        if (DebugMessages)
                        {
                            Core.Log.Info($"Deleting Zone");
                        }
                        zone.Close();
                    }
                }
            }
            if (HasBeenSieged && (DateTime.Now - AttackerLastFound).TotalMinutes >= 10 || DateTime.Now >= SafezoneUpAtThisTime)
            {
                HasBeenSieged = false;
                return Task.FromResult(true);
            }

            var attackers = FindAttackers(sphere, faction);

            if (HasBeenSieged)
            {
                if (attackers.Count > 0)
                {
                    AttackerLastFound = DateTime.Now;
                    return Task.FromResult(false);
                }

                if ((DateTime.Now - AttackerLastFound).TotalMinutes >= 10)
                {
                    HasBeenSieged = false;
                    return Task.FromResult(true);
                }
            }

            if (attackers.Count > 0)
            {
                //do stuff
                if (!HasBeenSieged)
                {
                    SafezoneUpAtThisTime = DateTime.Now.AddHours(2);
                    SafezoneDownAtThisTime = DateTime.Now.AddMinutes(5);
                    AttackerLastFound = DateTime.Now;
                    HasBeenSieged = true;
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(true);
        }

        private List<MyFaction> FindAttackers(BoundingSphereD sphere, MyFaction owner)
        {
            var foundAlliances = new List<MyFaction>();
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>().Where(x => x.BlocksCount >= 1))
            {
                if (grid.Projector != null)
                    continue;
                var fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                if (fac == null || (fac != null && fac.FactionId == owner.FactionId))
                {
                    continue;
                }

                if (!foundAlliances.Contains(fac))
                {
                    foundAlliances.Add(fac as MyFaction);
                }
            }

            return foundAlliances;
        }


        public DateTime NextLoop { get; set; }
        public int SecondsBetweenLoops { get; set; }
        public bool DebugMessages { get; set; } = false;
        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }

        public int Priority { get; set; } = 1;
    }
}
