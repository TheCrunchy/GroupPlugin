using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances.Upgrades;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using AlliancesPlugin.Territory_Version_2.Models;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRageMath;

namespace AlliancesPlugin.Territory_Version_2.SecondaryLogics
{
    public class ProductionBuffLogic : ISecondaryLogic
    {
        public bool Enabled { get; set; }
        public bool RequireOwner { get; set; }
        public Vector3 CentrePosition { get; set; }
        public int Radius = 2500;
        public double AssemblerSpeedBuff = 0.1;
        public double RefinerySpeedBuff = 0.1;
        public double RefineryYieldBuff = 0.1;
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
            var sphere = new BoundingSphereD(CentrePosition, Radius * 2);

            foreach (var refinery in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<IMyRefinery>())
            {
                ProductionBuffs.AddSpeedBuff(refinery.EntityId, RefinerySpeedBuff, this.SecondsBetweenLoops);
                ProductionBuffs.AddYieldBuff(refinery.EntityId, RefineryYieldBuff, this.SecondsBetweenLoops);
            }
            foreach (var assembler in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<IMyAssembler>())
            {
                ProductionBuffs.AddSpeedBuff(assembler.EntityId, AssemblerSpeedBuff, this.SecondsBetweenLoops);
            }
            return Task.FromResult(true);
        }

        public DateTime NextLoop { get; set; }
        public int SecondsBetweenLoops { get; set; } = 60;
        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }

        public int Priority { get; set; }
    }
}
