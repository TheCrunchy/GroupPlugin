using System;
using System.Linq;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using Territory.Territory_Version_2;
using Territory.Territory_Version_2.Interfaces;
using VRageMath;

namespace AlliancesPlugin.Territories.SecondaryLogics
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
        public Task<bool> DoSecondaryLogic(ICapLogic point, Territory.Territory_Version_2.Models.Territory territory)
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

        public int Priority { get; set; } = 1;
    }
}
