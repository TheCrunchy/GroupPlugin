using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances;
using AlliancesPlugin.Alliances.NewTerritories;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using AlliancesPlugin.Territory_Version_2.Models;
using AlliancesPlugin.Territory_Version_2.PointOwners;
using Newtonsoft.Json;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRageMath;

namespace AlliancesPlugin.Territory_Version_2.CapLogics
{
    public class AllianceGridCapLogic : ICapLogic
    {
        public string PointName = "Example name";
        public Vector3 GPSofPoint;
        public string GridOwnerTag = "SPRT";

        public int CaptureRadius = 5000;
        public List<ISecondaryLogic> SecondaryLogics { get; set; }
        public DateTime NextLoop { get; set; }
        public int SecondsBetweenLoops { get; set; } = 60;
        public int SuccessfulCapLockoutTimeSeconds = 3600;

        public int PointsToTake = 15;

        public Dictionary<Guid, int> Points = new Dictionary<Guid, int>();
        public IPointOwner PointOwner { get; set; }
        public Task<Tuple<bool, IPointOwner>> ProcessCap(ICapLogic point, Territory territory)
        {

            if (CanLoop())
            {
                NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
                //do capture logic for suits in alliances

                var gpspoint = GPSofPoint;
                if (gpspoint == new Vector3())
                {
                    AlliancePlugin.Log.Info($"GPS for point {PointName} is not set");
                    return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                }
                var sphere = new BoundingSphereD(gpspoint, CaptureRadius * 2);

                var foundAlliances = FindAttackers(sphere);
                var contested = foundAlliances.Contains(Guid.Empty) || foundAlliances.Count > 1;

                if (contested || !foundAlliances.Any())
                {
                    if (contested)
                    {
                        CaptureHandler.SendMessage($"Territory Capture {PointName}", $"Contested, point not captured.", territory, point.PointOwner ?? null);
                    }
                    return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                }

                var owner = foundAlliances.First();
                if (PointOwner != null)
                {
                    var currentPointOwner = PointOwner.GetOwner() as Alliance;
                    if (currentPointOwner != null && currentPointOwner.AllianceId == owner)
                    {
                        return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                    }
                }
                var pointOwner = new AlliancePointOwner()
                {
                    AllianceId = owner
                };
                AddToPoints(owner);
                var hasPoints = Points[owner];
                if (hasPoints < PointsToTake)
                {
                    CaptureHandler.SendMessage($"Territory Capture {PointName}", $"{AlliancePlugin.GetAllianceNoLoading(owner).name} Cap Progress {hasPoints}/{PointsToTake}", territory, point.PointOwner);
                    return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                }

                NextLoop = DateTime.Now.AddSeconds(SuccessfulCapLockoutTimeSeconds);

          
                CaptureHandler.SendMessage($"Territory Capture {PointName}", $"Captured by {AlliancePlugin.GetAllianceNoLoading(owner).name}, locking for {SuccessfulCapLockoutTimeSeconds / 60} Minutes", territory, point.PointOwner);
                this.PointOwner = pointOwner;
                return Task.FromResult(Tuple.Create<bool, IPointOwner>(true, pointOwner));
            }

            return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
        }

        private List<Guid> FindAttackers(BoundingSphereD sphere)
        {
            var foundAlliances = new List<Guid>();
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>().Where(x => x.BlocksCount >= 1))
            {
                if (grid.Projector != null)
                    continue;

                var fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                if ((fac != null && fac.Tag.Equals(GridOwnerTag)) || fac == null)
                {
                    continue;
                }

                var alliance = AlliancePlugin.GetAllianceNoLoading(fac as MyFaction);
                if (alliance == null)
                {
                    if (!foundAlliances.Contains(Guid.Empty))
                    {
                        foundAlliances.Add(Guid.Empty);
                    }
                }
                else
                {
                    if (!foundAlliances.Contains(alliance.AllianceId))
                    {
                        foundAlliances.Add(alliance.AllianceId);
                    }
                }
            }

            return foundAlliances;
        }

        private void AddToPoints(Guid owner)
        {
            if (Points.TryGetValue(owner, out int currentPoints))
            {
                Points[owner] = currentPoints + 1;
            }
            else
            {
                Points.Add(owner, 1);
            }
        }

        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }
    }
}
