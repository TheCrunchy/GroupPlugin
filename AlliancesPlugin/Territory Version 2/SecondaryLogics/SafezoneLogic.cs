using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using AlliancesPlugin.Territory_Version_2.Models;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRageMath;

namespace AlliancesPlugin.Territory_Version_2.SecondaryLogics
{
    public class SafezoneLogic : ISecondaryLogic
    {
        public bool Enabled { get; set; }
        public long SafezoneId { get; set; }
        public Vector3 SafezonePosition { get; set; }
        public Task<bool> DoSecondaryLogic(ICapLogic point, Territory territory)
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
                    AlliancePlugin.Log.Info($"Safezone Debug {point.PointName} owner is null");
                }
                if (foundzone != null)
                {
                    if (DebugMessages)
                    {
                        AlliancePlugin.Log.Info($"Safezone Debug {point.PointName} clearing the filters");
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
            var alliance = temp.GetOwner() as Alliance;
            if (alliance == null)
            {
                if (DebugMessages)
                {
                    AlliancePlugin.Log.Info($"Safezone Debug {point.PointName} Alliance is null");
                }
                return Task.FromResult(true);
            }
    
            if (foundzone != null)
            {
                if (DebugMessages)
                {
                    AlliancePlugin.Log.Info($"Safezone Debug {point.PointName} Using safezoneid from file, zone isnt null");
                }
                var zone = foundzone as MySafeZone;
                if (!CaptureHandler.TrackedSafeZoneIds.Contains(zone.EntityId))
                {
                    CaptureHandler.TrackedSafeZoneIds.Add(zone.EntityId);
                }
                zone.Factions.Clear();
                foreach (var member in alliance.AllianceMembers)
                {
                    var faction = MySession.Static.Factions.TryGetFactionById(member);
                    if (faction != null)
                    {
                        zone.Factions.Add(faction as MyFaction);
                    }
                }
                MySessionComponentSafeZones.RequestUpdateSafeZone((MyObjectBuilder_SafeZone)zone.GetObjectBuilder());
            }
            else
            {
                foreach (MySafeZone zone in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MySafeZone>())
                {
                    if (DebugMessages)
                    {
                        AlliancePlugin.Log.Info($"Safezone Debug {point.PointName} zone hasnt been found yet, {zone.EntityId} is a zone");
                    }
                    zone.Factions.Clear();
                    if (!CaptureHandler.TrackedSafeZoneIds.Contains(zone.EntityId))
                    {
                        CaptureHandler.TrackedSafeZoneIds.Add(zone.EntityId);
                    }
                    SafezoneId = zone.EntityId;
                    foreach (var member in alliance.AllianceMembers)
                    {
                        var faction = MySession.Static.Factions.TryGetFactionById(member);
                        if (faction != null)
                        {
                            zone.Factions.Add(faction as MyFaction);
                        }
                    }
                    MySessionComponentSafeZones.RequestUpdateSafeZone((MyObjectBuilder_SafeZone)zone.GetObjectBuilder());
                }

            }

            if (DebugMessages)
            {
                AlliancePlugin.Log.Info($"Safezone Debug {point.PointName} zone logic cycle done");
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
