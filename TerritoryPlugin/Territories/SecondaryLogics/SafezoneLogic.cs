using System;
using System.Linq;
using System.Threading.Tasks;
using CrunchGroup.Territories.Interfaces;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRageMath;

namespace CrunchGroup.Territories.SecondaryLogics
{
    public class SafezoneLogic : ISecondaryLogic
    {
        public bool Enabled { get; set; }
        public long SafezoneId { get; set; }
        public Vector3 SafezonePosition { get; set; }
        public Task<bool> DoSecondaryLogic(ICapLogic point, Models.Territory territory)
        {
            if (!Enabled)
            {
                return Task.FromResult(true);
            }

            if (!CanLoop()) return Task.FromResult(true);

            NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);

            IPointOwner temp = point.PointOwner ?? territory.Owner;
            BoundingSphereD sphere = new BoundingSphereD(SafezonePosition, 5000);
            var foundzone = MyAPIGateway.Entities.GetEntityById(SafezoneId);
            if (temp == null)
            {
                if (DebugMessages)
                {
                    GroupPlugin.Log.Info($"Safezone Debug {point.PointName} owner is null");
                }
                if (foundzone != null)
                {
                    if (DebugMessages)
                    {
                        GroupPlugin.Log.Info($"Safezone Debug {point.PointName} clearing the filters");
                    }
                    var zone = foundzone as MySafeZone;
                    if (!CaptureHandler.TrackedSafeZoneIds.Contains(zone.EntityId))
                    {
                        CaptureHandler.TrackedSafeZoneIds.Add(zone.EntityId);
                    }
                    zone.Factions.Clear();
                    MySessionComponentSafeZones.RequestUpdateSafeZone((MyObjectBuilder_SafeZone)zone.GetObjectBuilder());
                }

                return Task.FromResult(true);
            }
            var faction = temp.GetOwner() as MyFaction;
            if (faction == null)
            {
                if (DebugMessages)
                {
                    GroupPlugin.Log.Info($"Safezone Debug {point.PointName} Alliance is null");
                }
                return Task.FromResult(true);
            }

            if (foundzone != null)
            {
                if (DebugMessages)
                {
                    GroupPlugin.Log.Info($"Safezone Debug {point.PointName} Using safezoneid from file, zone isnt null");
                }
                var zone = foundzone as MySafeZone;
                if (!CaptureHandler.TrackedSafeZoneIds.Contains(zone.EntityId))
                {
                    CaptureHandler.TrackedSafeZoneIds.Add(zone.EntityId);
                }
                zone.Factions.Clear();

                zone.Factions.Add(faction);

                MySessionComponentSafeZones.RequestUpdateSafeZone((MyObjectBuilder_SafeZone)zone.GetObjectBuilder());
            }
            else
            {
                foreach (MySafeZone zone in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MySafeZone>())
                {
                    if (DebugMessages)
                    {
                        GroupPlugin.Log.Info($"Safezone Debug {point.PointName} zone hasnt been found yet, {zone.EntityId} is a zone");
                    }
                    zone.Factions.Clear();
                    if (!CaptureHandler.TrackedSafeZoneIds.Contains(zone.EntityId))
                    {
                        CaptureHandler.TrackedSafeZoneIds.Add(zone.EntityId);
                    }
                    SafezoneId = zone.EntityId;

                    zone.Factions.Add(faction);

                    MySessionComponentSafeZones.RequestUpdateSafeZone((MyObjectBuilder_SafeZone)zone.GetObjectBuilder());
                }

            }

            if (DebugMessages)
            {
                GroupPlugin.Log.Info($"Safezone Debug {point.PointName} zone logic cycle done");
            }
            return Task.FromResult(true);
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
