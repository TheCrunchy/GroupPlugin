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
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRageMath;

namespace AlliancesPlugin.Territory_Version_2.CapLogics
{
    public class BlockOwnershipLogic : ICapLogic
    {
        public void AddSecondaryLogic(ISecondaryLogic logic)
        {
            if (SecondaryLogics == null)
            {
                SecondaryLogics = new List<ISecondaryLogic>();
            }
            SecondaryLogics.Add(logic);
        }
        public long BlockId;
        public Task<Tuple<bool, IPointOwner>> ProcessCap(ICapLogic point, Territory territory)
        {
            if (CanLoop())
            {
                NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
                //   AlliancePlugin.Log.Info("Test 1");
                var gpspoint = GPSofPoint;
                if (gpspoint == new Vector3())
                {
                    AlliancePlugin.Log.Info($"GPS for point {PointName} is not set");
                    return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                }

                var sphere = new BoundingSphereD(gpspoint, DistanceCheck * 2);
                //    AlliancePlugin.Log.Info("Test 2");

                //     AlliancePlugin.Log.Info("Test 3");
                var foundAlliances = new List<Guid>();
                //      AlliancePlugin.Log.Info("Test 4");
                //     AlliancePlugin.Log.Info(blocks.Count);
                var entity = MyAPIGateway.Entities.GetEntityById(BlockId);
                if (entity != null)
                {
                    var block = entity as MyFunctionalBlock;
                    var fac = MySession.Static.Factions.TryGetFactionByTag(block.GetOwnerFactionTag());
                    if (fac != null)
                    {
                        var alliance = AlliancePlugin.GetAllianceNoLoading(fac);
                        if (alliance != null)
                        {

                            if (!foundAlliances.Contains(alliance.AllianceId))
                            {
                                foundAlliances.Add(alliance.AllianceId);
                            }
                        }
                    }
                }
                if (!foundAlliances.Any())
                {
                    var blocks = FindBlock(sphere);
                    foreach (var block in blocks)
                    {
                        var fac = MySession.Static.Factions.TryGetFactionByTag(block.GetOwnerFactionTag());
                        if (fac == null) continue;
                        var alliance = AlliancePlugin.GetAllianceNoLoading(fac);
                        if (alliance == null) continue;

                        if (!foundAlliances.Contains(alliance.AllianceId))
                        {
                            foundAlliances.Add(alliance.AllianceId);
                        }

                        BlockId = block.EntityId;
                        break;
                    }
                }

                //      AlliancePlugin.Log.Info("Test 5");
                if (!foundAlliances.Any())
                {
                    return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                }
                //     AlliancePlugin.Log.Info("Test 6");
                var owner = foundAlliances.First();
                if (PointOwner != null)
                {
                    var currentPointOwner = PointOwner.GetOwner() as Alliance;
                    if (currentPointOwner != null && currentPointOwner.AllianceId == owner)
                    {
                        return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                    }
                }
                //     AlliancePlugin.Log.Info("Test 7");
                var pointOwner = new AlliancePointOwner()
                {
                    AllianceId = owner
                };
                //   AlliancePlugin.Log.Info("Test 8");

                //   AlliancePlugin.Log.Info("Test 9");
                //AlliancePlugin.Log.Info(AlliancePlugin.GetAllianceNoLoading(owner).name);
                CaptureHandler.SendMessage($"Territory Capture {PointName}", $"Captured by {AlliancePlugin.GetAllianceNoLoading(owner).name}, locking for {SuccessfulCapLockoutTimeSeconds / 60} Minutes", territory, point.PointOwner);
                NextLoop = DateTime.Now.AddSeconds(SuccessfulCapLockoutTimeSeconds);

                //   AlliancePlugin.Log.Info("Test 10");
                this.PointOwner = pointOwner;
                return Task.FromResult(Tuple.Create<bool, IPointOwner>(true, pointOwner));
            }

            return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
        }

        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }
        public int SuccessfulCapLockoutTimeSeconds = 3600;
        public string PointName { get; set; } = "Example name";
        public Vector3 GPSofPoint;
        public int DistanceCheck = 1000;
        public string ClaimBlockSubType = "claimblock";
        public List<ISecondaryLogic> SecondaryLogics { get; set; }
        public DateTime NextLoop { get; set; }
        public int SecondsBetweenLoops { get; set; }
        public IPointOwner PointOwner { get; set; }

        private List<MyCubeBlock> FindBlock(BoundingSphereD sphere)
        {
            List<MyCubeBlock> Found = new List<MyCubeBlock>();
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyFunctionalBlock>()
                         .Where(x => x.SlimBlock?.BlockDefinition?.Id.SubtypeName == this.ClaimBlockSubType))
            {
                Found.Add(grid);
            }
            return Found;
        }
    }
}
