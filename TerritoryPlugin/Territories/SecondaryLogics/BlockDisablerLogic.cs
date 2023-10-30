using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Territory.Territories.Interfaces;
using VRage.Game;
using VRageMath;

namespace Territory.Territories.SecondaryLogics
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
        public List<String> TargetedSubtypes;
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
                if (grid.GridSizeEnum == MyCubeSize.Large && !DisableLargeGrid)
                {
                    continue;
                }
                if (grid.GridSizeEnum == MyCubeSize.Small && !DisableSmallGrid)
                {
                    continue;
                }
                foreach (var block in grid.GetFatBlocks().Where(block => block.BlockDefinition.Id != null &&
                                                                         TargetedSubtypes.Contains(block.BlockDefinition.Id.SubtypeId.ToString())))
                {
                    FunctionalBlockPatch.AddBlockToDisable(block.EntityId, this.SecondsBetweenLoops);
                }
            }
            return Task.FromResult(true);
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
