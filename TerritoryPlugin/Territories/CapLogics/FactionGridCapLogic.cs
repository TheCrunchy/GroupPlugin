using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Territory.Territories.Interfaces;
using Territory.Territories.PointOwners;
using VRage.Game;
using VRageMath;

namespace Territory.Territories.CapLogics
{
    public class FactionGridCapLogic : ICapLogic
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

        public Vector3 GPSofPoint;
        public string GridOwnerTag = "SPRT";

        public int CaptureRadius = 5000;
        public List<ISecondaryLogic> SecondaryLogics { get; set; }
        public DateTime NextLoop { get; set; }
        public int SecondsBetweenLoops { get; set; } = 60;
        public int SuccessfulCapLockoutTimeSeconds = 3600;

        public int PointsToTake = 15;

        public Dictionary<long, int> Points = new Dictionary<long, int>();
        public IPointOwner PointOwner { get; set; }

        public bool AllowSmallGrid { get; set; }

        public Task<Tuple<bool, IPointOwner>> ProcessCap(ICapLogic point, Models.Territory territory)
        {

            if (CanLoop())
            {
                NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
                //do capture logic for suits in alliances

                var gpspoint = GPSofPoint;
                if (gpspoint == new Vector3())
                {
                    TerritoryPlugin.Log.Info($"GPS for point {PointName} is not set");
                    return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                }
                var sphere = new BoundingSphereD(gpspoint, CaptureRadius * 2);

                var foundAlliances = FindAttackers(sphere);
                var contested = foundAlliances.Count > 1;
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
                    var currentPointOwner = PointOwner.GetOwner() as MyFaction;
                    if (currentPointOwner != null && currentPointOwner.FactionId == owner.FactionId)
                    {
                        return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                    }
                }
                var pointOwner = new FactionPointOwner()
                {
                    FactionId = owner.FactionId
                };
                AddToPoints(owner.FactionId);
                var hasPoints = Points[owner.FactionId];
                if (hasPoints < PointsToTake)
                {
                    CaptureHandler.SendMessage($"Territory Capture {PointName}", $"{owner.Name} Cap Progress {hasPoints}/{PointsToTake}", territory, point.PointOwner);
                    return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                }

                NextLoop = DateTime.Now.AddSeconds(SuccessfulCapLockoutTimeSeconds);

                CaptureHandler.SendMessage($"Territory Capture {PointName}", $"Captured by {owner.Name}, locking for {SuccessfulCapLockoutTimeSeconds / 60} Minutes", territory, point.PointOwner);
                this.PointOwner = pointOwner;
                return Task.FromResult(Tuple.Create<bool, IPointOwner>(true, pointOwner));
            }

            return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
        }

        private List<MyFaction> FindAttackers(BoundingSphereD sphere)
        {
            var foundAlliances = new List<MyFaction>();
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


                if (!foundAlliances.Contains(fac))
                {
                    foundAlliances.Add(fac as MyFaction);
                }
            }

            return foundAlliances;
        }

        private void AddToPoints(long owner)
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
