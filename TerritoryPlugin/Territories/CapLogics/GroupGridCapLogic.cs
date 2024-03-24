using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrunchGroup.Handlers;
using CrunchGroup.Models;
using CrunchGroup.NexusStuff;
using CrunchGroup.Territories.Interfaces;
using CrunchGroup.Territories.PointOwners;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game;
using VRageMath;

namespace CrunchGroup.Territories.CapLogics
{
    public class GroupGridCapLogic : ICapLogic
    {

        public string PointName { get; set; } = "Example name";
        public void AddSecondaryLogic(ISecondaryLogic logic)
        {
            if (SecondaryLogics == null)
            {
                SecondaryLogics = new List<ISecondaryLogic>();
            }
            SecondaryLogics.Add(logic);
        }
        public void SetPosition(Vector3 position)
        {
            this.GPSofPoint = position;
        }
        public Vector3 GetPointsLocationIfSet()
        {
            return GPSofPoint;
        }

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

        public bool AllowSmallGrid { get; set; }

        public Task<Tuple<bool, IPointOwner>> ProcessCap(ICapLogic point, Models.Territory territory)
        {

            if (CanLoop())
            {
                NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
                var gpspoint = GPSofPoint;
                if (gpspoint == new Vector3())
                {
                    Core.Log.Info($"GPS for point {PointName} is not set");
                    return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                }
                //do capture logic for suits in alliances
                if (Core.NexusInstalled)
                {
                    var thisSector = NexusAPI.GetThisServer();

                    var sector = NexusAPI.GetServerIDFromPosition(point.GetPointsLocationIfSet());

                    if (sector != thisSector.ServerID)
                    {
                        return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                    }
                }
       
                var sphere = new BoundingSphereD(gpspoint, CaptureRadius * 2);

                var foundAlliances = FindAttackers(sphere);
                var contested = foundAlliances.Count > 1 || foundAlliances.Contains(Guid.Empty);
                if (!foundAlliances.Any())
                {
                    return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                }

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
                    var currentPointOwner = PointOwner.GetOwner() as Group;
                    if (currentPointOwner != null && currentPointOwner.GroupId == owner)
                    {
                        return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                    }
                }
                var pointOwner = new GroupPointOwner()
                {
                    GroupId = owner
                };
                var newOwner = pointOwner.GetOwner() as Group;
                if (newOwner == null)
                {
                    return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                }
                AddToPoints(owner);
                var hasPoints = Points[owner];
                if (hasPoints < PointsToTake)
                {
                    CaptureHandler.SendMessage($"Territory Capture {PointName}", $"{owner} Cap Progress {hasPoints}/{PointsToTake}", territory, point.PointOwner);
                    return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                }

                NextLoop = DateTime.Now.AddSeconds(SuccessfulCapLockoutTimeSeconds);

                CaptureHandler.SendMessage($"Territory Capture {PointName}", $"Captured by {newOwner.GroupName}, locking for {SuccessfulCapLockoutTimeSeconds / 60} Minutes", territory, point.PointOwner);
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

                if (grid.GridSizeEnum == MyCubeSize.Small && !AllowSmallGrid)
                {
                    continue;
                }

                var fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                if ((fac != null && fac.Tag.Equals(GridOwnerTag)) || fac == null)
                {
                    continue;
                }

                var group = GroupHandler.GetFactionsGroup(fac.FactionId);
                if (group == null)
                {
                    foundAlliances.Add(Guid.Empty);
                }
                else
                {
                    if (!foundAlliances.Contains(group.GroupId))
                    {
                        foundAlliances.Add(group.GroupId);
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
