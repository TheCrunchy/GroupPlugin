using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using AlliancesPlugin.Territory_Version_2.Models;
using AlliancesPlugin.Territory_Version_2.PointOwners;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using AllianceChat = AlliancesPlugin.Alliances.AllianceChat;

namespace AlliancesPlugin.Territory_Version_2.SecondaryLogics
{
    public class RadarLogic : ISecondaryLogic
    {
        public bool Enabled { get; set; }
        public bool RequireOwner = true;
        public Vector3 RadarCentre { get; set; }
        public int Distance { get; set; }
        public string IgnoredFactionTags = "SPRT,TAG2";
        private List<MyCubeGrid> FoundGrids = new List<MyCubeGrid>();
        public Task<bool> DoSecondaryLogic(ICapLogic point, Territory territory)
        {
            if (!Enabled)
            {
                return Task.FromResult(true);
            }

            if (!CanLoop()) return Task.FromResult(true);

            NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
            if (RequireOwner && point.PointOwner == null)
            {
                return Task.FromResult(true);
            }
            var temp = point.PointOwner ?? territory.Owner;

            var owner = temp.GetOwner();

            StringBuilder builder = new StringBuilder();
            FindGrids();
            foreach (var grid in FoundGrids)
            {
                if (!ShouldTriggerAlert(owner, grid)) continue;
              
                var gps = GPSHelper.CreateGps(grid.PositionComp.GetPosition(), Color.Cyan, $"Radar Hit", $"Radar {grid.DisplayName} at {DateTime.Now:HH:mm:ss zz}");
                if (owner is Alliance alliance)
                {
                    AllianceChat.SendChatMessage(alliance.AllianceId, "Radar", gps.ToString(), false, 0);
                }
                builder.AppendLine(gps.ToString());
            }

            CaptureHandler.SendRadarMessage(owner, builder.ToString());
            return Task.FromResult(true);
        }

        private bool ShouldTriggerAlert(Object pointOwner, MyCubeGrid grid)
        {
            var gridOwner = FacUtils.GetOwner(grid);
            var fac = FacUtils.GetPlayersFaction(gridOwner);
            if (fac == null)
            {
                return true;
            }
          //  AlliancePlugin.Log.Info("Radar 1");
            switch (pointOwner)
            {
                case Alliance alliance:
                    {
                     //   AlliancePlugin.Log.Info("Radar 2");
                        if (!alliance.AllianceMembers.Contains(fac.FactionId))
                        {
                            return true;
                        }
                    }
                    break;
                case IMyFaction faction:
                    {
                   //     AlliancePlugin.Log.Info("Radar 3");
                        if (fac.FactionId != faction.FactionId)
                        {
                            if (!MySession.Static.Factions.AreFactionsFriends(fac.FactionId, faction.FactionId) || !MySession.Static.Factions.AreFactionsNeutrals(fac.FactionId, faction.FactionId))
                            {
                                return true;
                            }
                        }
                    }
                    break;
            }

            return false;
        }

        public int MinimumBlocksToHit = 1;
        public DateTime NextLoop { get; set; }
        public int SecondsBetweenLoops { get; set; }
        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }

        public int Priority { get; set; }

        public void FindGrids()
        {
            FoundGrids.Clear();
          //  AlliancePlugin.Log.Info("grid 1");
            var sphere = new BoundingSphereD(RadarCentre, Distance * 2);
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>().Where(x => x.Projector == null && x.BlocksCount >= MinimumBlocksToHit))
            {
                //       AlliancePlugin.Log.Info("grid 2");
                var owner = FacUtils.GetOwner(grid);
           //     AlliancePlugin.Log.Info("grid 3");
                var fac = FacUtils.GetPlayersFaction(owner);
            //    AlliancePlugin.Log.Info("grid 4");
                if ((fac != null && IgnoredFactionTags.Contains(fac.Tag)))
                {
            //        AlliancePlugin.Log.Info("grid 5");
                    continue;
                }
           //     AlliancePlugin.Log.Info("grid 6");

                FoundGrids.Add(grid);
            }
        }
    }
}
