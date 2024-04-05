using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup.Models;
using CrunchGroup.Territories.Interfaces;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace CrunchGroup.Territories.SecondaryLogics
{
    public class RadarLogic : ISecondaryLogic
    {
        public bool Enabled { get; set; }
        public bool RequireOwner = true;
        public Vector3 RadarCentre { get; set; }
        public int Distance { get; set; }
        public string IgnoredFactionTags = "SPRT,TAG2";
        private List<MyCubeGrid> FoundGrids = new List<MyCubeGrid>();
        public Task<bool> DoSecondaryLogic(ICapLogic point, Models.Territory territory)
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
                if (owner is MyFaction fac)
                {
                    Core.Log.Error("This isnt setup for factions");
                    //not setup here
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
            //  GroupPlugin.Log.Info("Radar 1");
            switch (pointOwner)
            {
                case IMyFaction faction:
                    {
                        //     GroupPlugin.Log.Info("Radar 3");
                        if (fac.FactionId != faction.FactionId)
                        {
                            if (!MySession.Static.Factions.AreFactionsFriends(fac.FactionId, faction.FactionId) || !MySession.Static.Factions.AreFactionsNeutrals(fac.FactionId, faction.FactionId))
                            {
                                return true;
                            }
                        }
                    }
                    break;
                case Group group:
                    {
                        //     GroupPlugin.Log.Info("Radar 3");
                        if (!group.GroupMembers.Contains(fac.FactionId))
                        {
                            return true;

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

        public int Priority { get; set; } = 1;

        public void FindGrids()
        {
            FoundGrids.Clear();
            //  GroupPlugin.Log.Info("grid 1");
            var sphere = new BoundingSphereD(RadarCentre, Distance * 2);
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>().Where(x => x.Projector == null && x.BlocksCount >= MinimumBlocksToHit))
            {
                //       GroupPlugin.Log.Info("grid 2");
                var owner = FacUtils.GetOwner(grid);
                //     GroupPlugin.Log.Info("grid 3");
                var fac = FacUtils.GetPlayersFaction(owner);
                //    GroupPlugin.Log.Info("grid 4");
                if ((fac != null && IgnoredFactionTags.Contains(fac.Tag)))
                {
                    //        GroupPlugin.Log.Info("grid 5");
                    continue;
                }
                //     GroupPlugin.Log.Info("grid 6");

                FoundGrids.Add(grid);
            }
        }
    }
}
