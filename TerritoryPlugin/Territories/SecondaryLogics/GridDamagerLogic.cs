﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrunchGroup.Territories.Interfaces;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRageMath;

namespace CrunchGroup.Territories.SecondaryLogics
{
    public class GridDamagerLogic : ISecondaryLogic
    {
        public int Priority { get; set; } = 1;
        public Vector3 CentrePosition { get; set; }
        public int Distance { get; set; }
        public string IgnoredFactionTags = "SPRT,TAG2";
        private List<MyCubeGrid> FoundGrids = new List<MyCubeGrid>();
        public List<String> TargetedSubtypes;
        public bool RequireOwner { get; set; }
        public bool Enabled { get; set; }
        public float Damage = 1000;
        public Task<bool> DoSecondaryLogic(ICapLogic point, Models.Territory territory)
        {
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

            var explodeThese = new List<MyCubeBlock>();
            FindGrids();
            foreach (var grid in FoundGrids)
            {
                foreach (var block in grid.GetFatBlocks().Where(block => block.BlockDefinition.Id != null &&
                                                                         TargetedSubtypes.Contains(block.BlockDefinition.Id.SubtypeId.ToString())))
                {
                    explodeThese.Add(block);
                }
            }
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                foreach (var block in explodeThese)
                {
                    block.SlimBlock.DoDamage(Damage, MyDamageType.Fire);
                }
            });

            return Task.FromResult(true);
        }

        public DateTime NextLoop { get; set; }
        public int SecondsBetweenLoops { get; set; }
        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }

        public void FindGrids()
        {
            FoundGrids.Clear();
            var sphere = new BoundingSphereD(CentrePosition, Distance * 2);
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>().Where(x => x.Projector == null && x.BlocksCount >= 1))
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
