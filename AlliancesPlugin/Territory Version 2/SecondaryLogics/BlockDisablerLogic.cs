using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using AlliancesPlugin.Territory_Version_2.Models;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using VRage.Game;
using VRageMath;

namespace AlliancesPlugin.Territory_Version_2.SecondaryLogics
{
    public class BlockDisablerLogic : ISecondaryLogic
    {
        public bool Enabled { get; set; }
        public bool RequireOwner = true;
        public Vector3 CentrePosition { get; set; }
        public int Distance { get; set; }
        public string IgnoredFactionTags = "SPRT,TAG2";
        private List<MyCubeGrid> FoundGrids = new List<MyCubeGrid>();

        public bool DisableLargeGrid = true;
        public bool DisableSmallGrid = true;

        public Task<bool> DoSecondaryLogic(ICapLogic point, Territory territory)
        {
            //this crashes server 
            if (!Enabled)
            {
                return Task.FromResult(true);
            }

            if (!CanLoop()) return Task.FromResult(true);
        //    AlliancePlugin.Log.Info("1");
            NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
            if (RequireOwner && point.PointOwner == null)
            {
                return Task.FromResult(true);
            }

  //         AlliancePlugin.Log.Info("2");
            var temp = point.PointOwner ?? territory.Owner;
    //        AlliancePlugin.Log.Info("3");
            var owner = temp.GetOwner();
       //     AlliancePlugin.Log.Info("4");
            StringBuilder builder = new StringBuilder();
            FindGrids();
        //    AlliancePlugin.Log.Info("5");

        //    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
        //    {
                if (DisableLargeGrid)
                {
                    foreach (var grid in FoundGrids.Where(x => x.GridSizeEnum == MyCubeSize.Large))
                    {
                      //  AlliancePlugin.Log.Info("6");
                        foreach (MyCubeBlock block in grid.GetFatBlocks())
                        {
                         //   AlliancePlugin.Log.Info("7");
                            if (block is MyFunctionalBlock func)
                            {
                               // AlliancePlugin.Log.Info("8");
                                FunctionalBlockPatch.AddBlockToDisable(block.EntityId, this.SecondsBetweenLoops);
                            }
                        }
                    }
                }

                if (DisableSmallGrid)
                {
                    foreach (var grid in FoundGrids.Where(x => x.GridSizeEnum == MyCubeSize.Small))
                    {
                        foreach (MyCubeBlock block in grid.GetFatBlocks())
                        {
                            if (block is MyFunctionalBlock func)
                            {
                                FunctionalBlockPatch.AddBlockToDisable(block.EntityId, this.SecondsBetweenLoops);
                            }
                        }
                    }
                }
       //     });

            return Task.FromResult(true);
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
            var sphere = new BoundingSphereD(CentrePosition, Distance * 2);
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>().Where(x => x.Projector == null && x.BlocksCount >= MinimumBlocksToHit))
            {
                var owner = FacUtils.GetOwner(grid);
                var fac = FacUtils.GetPlayersFaction(owner);
                if ((fac != null && IgnoredFactionTags.Contains(fac.Tag)))
                {
                    continue;
                }
                FoundGrids.Add(grid);
            }
        }
    }
}
