using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup.NexusStuff;
using CrunchGroup.Territories.Interfaces;
using CrunchGroup.Territories.Models;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Components;
using VRage.ObjectBuilders;
using VRageMath;

namespace CrunchGroup.Territories.SecondaryLogics
{
    public class DarksSafezoneLogic : ISecondaryLogic
    {
        public bool Enabled { get; set; }
        private MySafeZone Safezone { get; set; }

        public Task<bool> DoSecondaryLogic(ICapLogic point, Models.Territory territory)
        {
            if (!Enabled)
            {
                return Task.FromResult(true);
            }

            if (!CanLoop()) return Task.FromResult(true);
            if (Core.NexusInstalled)
            {
                var thisSector = NexusAPI.GetThisServer();
                if (DebugMessages)
                {
                    Core.Log.Info($"{thisSector.ServerID}");
                }

                var sector = NexusAPI.GetServerIDFromPosition(point.GetPointsLocationIfSet());
                if (DebugMessages)
                {
                    Core.Log.Info($"{sector}");
                }

                if (sector != thisSector.ServerID)
                {
                    if (DebugMessages)
                    {
                        Core.Log.Info("Not this sector, returning");
                        return Task.FromResult(true);
                    }
                }
            }
            NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
            
            IPointOwner temp = point.PointOwner ?? territory.Owner;
            BoundingSphereD sphere = new BoundingSphereD(point.GetPointsLocationIfSet(), 5000);
            var foundzone = Safezone;
            if (temp == null)
            {
                if (DebugMessages)
                {
                    Core.Log.Info($"Safezone Debug {point.PointName} owner is null");
                }
                if (foundzone != null)
                {
                    var zone = foundzone;
                    zone?.Close();
                }
                return Task.FromResult(true);
            }

            var faction = temp.GetOwner() as MyFaction;
            if (faction == null)
            {
                if (DebugMessages)
                {
                    Core.Log.Info($"Safezone Debug {point.PointName} faction is null");
                }
                return Task.FromResult(true);
            }

            if (foundzone != null)
            {
                if (DebugMessages)
                {
                    Core.Log.Info($"Safezone Debug {point.PointName} Using safezoneid from file, zone isnt null");
                }
                var zone = foundzone as MySafeZone;
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
                        Core.Log.Info($"Safezone Debug {point.PointName} zone hasnt been found yet, {zone.EntityId} is a zone");
                    }
                    zone.Factions.Clear();
                    Safezone = zone;
                    zone.Factions.Add(faction);

                    MySessionComponentSafeZones.RequestUpdateSafeZone((MyObjectBuilder_SafeZone)zone.GetObjectBuilder());
                }
            }

            if (Safezone == null)
            {
                MyObjectBuilder_SafeZone objectBuilderSafeZone = new MyObjectBuilder_SafeZone();
                objectBuilderSafeZone.PositionAndOrientation = new MyPositionAndOrientation?(new MyPositionAndOrientation(point.GetPointsLocationIfSet(), Vector3.Forward, Vector3.Up));
                objectBuilderSafeZone.PersistentFlags = MyPersistentEntityFlags2.InScene;
                objectBuilderSafeZone.Shape = MySafeZoneShape.Sphere;
                objectBuilderSafeZone.Radius = (float)5000;
                objectBuilderSafeZone.Enabled = true;
                objectBuilderSafeZone.DisplayName = $"{point.PointName} Safezone";
                objectBuilderSafeZone.ModelColor = Color.Green.ToVector3();
                objectBuilderSafeZone.AllowedActions = MySafeZoneAction.Drilling | MySafeZoneAction.Building  | MySafeZoneAction.Grinding | MySafeZoneAction.Welding | MySafeZoneAction.LandingGearLock;
                objectBuilderSafeZone.AccessTypeGrids = MySafeZoneAccess.Blacklist;
                objectBuilderSafeZone.AccessTypeFloatingObjects = MySafeZoneAccess.Blacklist;
                objectBuilderSafeZone.AccessTypeFactions = MySafeZoneAccess.Whitelist;
                if (DebugMessages)
                {
                    Core.Log.Info("Spawning the safezone");
                }
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    MyEntity ent =
                        Sandbox.Game.Entities.MyEntities.CreateFromObjectBuilderAndAdd(
                            (MyObjectBuilder_EntityBase)objectBuilderSafeZone, true);
                    Safezone = (MySafeZone)ent;
                });

            }

            if (DebugMessages)
            {
                Core.Log.Info($"Safezone Debug {point.PointName} zone logic cycle done");
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
