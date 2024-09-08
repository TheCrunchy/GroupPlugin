using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup.NexusStuff;
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
        public bool HasBeenSieged { get; set; }
        public DateTime SafezoneUpAtThisTime { get; set; }
        public DateTime SafezoneDownAtThisTime { get; set; }

        public DateTime AttackerLastFound { get; set; }
        public DateTime CooldownUntil { get; set; }

        public int WarmupMinutes { get; set; } = 5;
        public int SafezoneDownForMinutes { get; set; } = 120;
        public int CooldownMinutes { get; set; } = 120;
        public int EnemySearchDistance { get; set; } = 10000;

        public string IgnoredGridOwnerTag = "SPRT";

        public Task<bool> DoSecondaryLogic(ICapLogic point, Models.Territory territory)
        {
            if (!Enabled)
            {
                return Task.FromResult(true);
            }

            if (!CanLoop()) return Task.FromResult(false);

            NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
            if (DateTime.Now < CooldownUntil && !HasBeenSieged)
            {
                return Task.FromResult(true);
            }
            if (Core.NexusInstalled)
            {
                var thisSector = NexusAPI.GetThisServer();

                var sector = NexusAPI.GetServerIDFromPosition(point.GetPointsLocationIfSet());

                if (sector != thisSector.ServerID)
                {
                    return Task.FromResult(true);
                }
            }
            IPointOwner temp = point.PointOwner;
            BoundingSphereD sphere = new BoundingSphereD(point.GetPointsLocationIfSet(), EnemySearchDistance);
        
            if (temp == null)
            {
                if (DebugMessages)
                {
                    Core.Log.Info($"Safezone Enemy Debug {point.PointName} owner is null");
                }
                return Task.FromResult(false);
            }

            var faction = temp.GetOwner() as MyFaction;
            if (faction == null)
            {
                if (DebugMessages)
                {
                    Core.Log.Info($"Safezone Enemy Debug {point.PointName} faction is null");
                }
                return Task.FromResult(false);
            }

            //find some online members 
            var defenders = new List<long>();
   
            if (Core.NexusInstalled)
            {
                var members = NexusAPI.GetAllOnlinePlayers();
                defenders.AddRange(members
                    .Where(x => MySession.Static.Factions.TryGetPlayerFaction(x.IdentityID) != null
                                && MySession.Static.Factions.TryGetPlayerFaction(x.IdentityID).FactionId ==
                                faction.FactionId).Select(x => x.IdentityID));
            }
            else if (Core.NexusGlobalAPI.Enabled)
            {
                var members = Core.NexusGlobalAPI.GetAllOnlinePlayers();

                var identities = new List<long>();

                foreach (var member in members)
                {
                    var identity = MySession.Static.Players.TryGetPlayerIdentity(member);
                    if (identity != null)
                    {
                        identities.Add(identity.IdentityId);
                    }
                }
                defenders.AddRange(identities
                    .Where(x => MySession.Static.Factions.TryGetPlayerFaction(x) != null
                                && MySession.Static.Factions.TryGetPlayerFaction(x).FactionId ==
                                faction.FactionId).Select(x => x));
            }
            else
            {
                defenders.AddRange(MySession.Static.Players.GetOnlinePlayers()
                    .Where(x => MySession.Static.Factions.TryGetPlayerFaction(x.Identity.IdentityId) != null
                                && MySession.Static.Factions.TryGetPlayerFaction(x.Identity.IdentityId).FactionId ==
                                faction.FactionId).Select(x => x.Identity.IdentityId));
            }
            if (!defenders.Any())
            {
                if (DebugMessages)
                {
                    Core.Log.Info($"Safezone Enemy Debug {point.PointName} no online defenders in instance");
                }
                return Task.FromResult(HasBeenSieged);
            }
            //check if defenders are online 

            if (DebugMessages)
            {
                Core.Log.Info($"Safezone enemy Debug {point.PointName} zone logic cycle done");
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
                        SafezoneUpAtThisTime = DateTime.Now.AddMinutes(SafezoneDownForMinutes);
      
                        zone.Close();
                        CaptureHandler.SendMessage($"{point.PointName}", $"{point.PointName} Safezone has dropped, siege will last for {SafezoneDownForMinutes} minutes.", territory, temp);
                    }
                }
            }
            if (HasBeenSieged && DateTime.Now >= SafezoneUpAtThisTime)
            {
                CaptureHandler.SendMessage($"{point.PointName}", $"{point.PointName} Siege is ended, safezone will return shortly, siege cooldown for {CooldownMinutes} minutes.", territory, temp);
                CooldownUntil = DateTime.Now.AddMinutes(CooldownMinutes);
                HasBeenSieged = false;
                return Task.FromResult(true);
            }

            var attackers = FindAttackers(sphere, faction);

            if (HasBeenSieged)
            {
                if (attackers.Count > 0)
                {
                    AttackerLastFound = DateTime.Now;
                }

                if ((DateTime.Now - AttackerLastFound).TotalMinutes >= 10)
                {
                    CaptureHandler.SendMessage($"{point.PointName}", $"{point.PointName} Attackers abandoned siege, siege is ended. Siege cooldown for {CooldownMinutes} minutes.", territory, temp);
                    CooldownUntil = DateTime.Now.AddMinutes(CooldownMinutes);
                    HasBeenSieged = false;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }

            if (attackers.Count > 0)
            {
                //do stuff
                if (!HasBeenSieged)
                {
           
                    SafezoneDownAtThisTime = DateTime.Now.AddMinutes(WarmupMinutes);
                    AttackerLastFound = DateTime.Now;
                    SafezoneUpAtThisTime = DateTime.Now.AddMinutes(SafezoneDownForMinutes);
                    HasBeenSieged = true;
                    CaptureHandler.SendMessage($"{point.PointName}", $"{point.PointName} {faction.Name} Safezone will drop in {WarmupMinutes} minutes, attacked by {string.Join(", ", attackers.Select(x => x.Name))}", territory, temp);
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

                if (IgnoredGridOwnerTag.Contains(fac.Tag))
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
