﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup.Territories.Interfaces;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRageMath;

namespace CrunchGroup.Territories.SecondaryLogics
{
    public class ThrustDisablerLogic : ISecondaryLogic
    {
        public bool Enabled { get; set; }
        public bool RequireOwner = true;
        public Vector3 CentrePosition { get; set; }
        public int Distance { get; set; }
        public string IgnoredFactionTags = "SPRT,TAG2";
        private List<MyCubeGrid> FoundGrids = new List<MyCubeGrid>();

        public bool DisableLargeGrid = true;
        public bool DisableSmallGrid = true;

        public string DisabledDirections = "UP,DOWN,FORWARD,BACKWARD,LEFT,RIGHT";

        public Task<bool> DoSecondaryLogic(ICapLogic point, Models.Territory territory)
        {
            //this crashes server 
            if (!Enabled)
            {
                return Task.FromResult(true);
            }

            if (!CanLoop()) return Task.FromResult(true);
            //    GroupPlugin.Log.Info("1");
            NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
            if (RequireOwner && point.PointOwner == null)
            {
                return Task.FromResult(true);
            }

            //         GroupPlugin.Log.Info("2");
            var temp = point.PointOwner ?? territory.Owner;
            //        GroupPlugin.Log.Info("3");
            var owner = temp.GetOwner();
            //     GroupPlugin.Log.Info("4");
            StringBuilder builder = new StringBuilder();
            FindGrids();
            //    GroupPlugin.Log.Info("5");

            //    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            //    {
            if (DisableLargeGrid)
            {
                foreach (var grid in FoundGrids.Where(x => x.GridSizeEnum == MyCubeSize.Large))
                {
                    //  GroupPlugin.Log.Info("6");
                    foreach (MyThrust block in grid.GetFatBlocks().Where(x => x.BlockDefinition != null && x.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_Thrust)))
                    {
                     //   GroupPlugin.Log.Info("thruster large");
                        if (block.GridThrustDirection == Vector3I.Backward && !DisabledDirections.Contains("BACKWARD"))
                        {
                          //  GroupPlugin.Log.Info("BACKWARD");
                            continue;
                        }
                        if (block.GridThrustDirection == Vector3I.Forward && !DisabledDirections.Contains("FORWARD"))
                        {
                           // GroupPlugin.Log.Info("FORWARD");
                            continue;
                        }
                        if (block.GridThrustDirection == Vector3I.Left && !DisabledDirections.Contains("LEFT"))
                        {
                            continue;
                        }
                        if (block.GridThrustDirection == Vector3I.Right && !DisabledDirections.Contains("RIGHT"))
                        {
                            continue;
                        }
                        if (block.GridThrustDirection == Vector3I.Up && !DisabledDirections.Contains("UP"))
                        {
                            continue;
                        }
                        if (block.GridThrustDirection == Vector3I.Down && !DisabledDirections.Contains("DOWN"))
                        {
                            continue;
                        }
                        FunctionalBlockPatch.AddBlockToDisable(block.EntityId, this.SecondsBetweenLoops);
                    }
                }
            }

            if (DisableSmallGrid)
            {
                foreach (var grid in FoundGrids.Where(x => x.GridSizeEnum == MyCubeSize.Small))
                {
                    foreach (MyThrust block in grid.GetFatBlocks().Where(x => x.BlockDefinition != null && x.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_Thrust)))
                    {
                        if (block.GridThrustDirection == Vector3I.Backward && !DisabledDirections.Contains("BACKWARD"))
                        {
                            //  GroupPlugin.Log.Info("BACKWARD");
                            continue;
                        }
                        if (block.GridThrustDirection == Vector3I.Forward && !DisabledDirections.Contains("FORWARD"))
                        {
                            // GroupPlugin.Log.Info("FORWARD");
                            continue;
                        }
                        if (block.GridThrustDirection == Vector3I.Left && !DisabledDirections.Contains("LEFT"))
                        {
                            continue;
                        }
                        if (block.GridThrustDirection == Vector3I.Right && !DisabledDirections.Contains("RIGHT"))
                        {
                            continue;
                        }
                        if (block.GridThrustDirection == Vector3I.Up && !DisabledDirections.Contains("UP"))
                        {
                            continue;
                        }
                        if (block.GridThrustDirection == Vector3I.Down && !DisabledDirections.Contains("DOWN"))
                        {
                            continue;
                        }
                        FunctionalBlockPatch.AddBlockToDisable(block.EntityId, this.SecondsBetweenLoops);
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
