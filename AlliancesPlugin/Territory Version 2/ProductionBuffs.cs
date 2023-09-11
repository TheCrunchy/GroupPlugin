using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using VRageMath;

namespace AlliancesPlugin.Alliances.Upgrades
{
    public static class ProductionBuffs
    {

        private static Dictionary<long, double> SpeedBuffs = new Dictionary<long, double>();
        private static Dictionary<long, double> YieldBuffs = new Dictionary<long, double>();
        private static Dictionary<long, DateTime> RemoveAt = new Dictionary<long, DateTime>();

        public static void AddSpeedBuff(long entId, double buff, double secondsBetween)
        {
            if (SpeedBuffs.ContainsKey(entId))
            {
                SpeedBuffs[entId] = buff;
                RemoveAt[entId] = DateTime.Now.AddSeconds(secondsBetween + 5);
                return;
            }
            SpeedBuffs.Add(entId, buff);
            if (RemoveAt.ContainsKey(entId))
            {
                RemoveAt[entId] = DateTime.Now.AddSeconds(secondsBetween + 5);
            }
            else
            {
                RemoveAt.Add(entId, DateTime.Now.AddSeconds(secondsBetween + 5));
            }
            
        }
        public static void AddYieldBuff(long entId, double buff, double secondsBetween)
        {
            if (YieldBuffs.ContainsKey(entId))
            {
                YieldBuffs[entId] = buff;
                RemoveAt[entId] = DateTime.Now.AddSeconds(secondsBetween + 5);
                return;
            }
            YieldBuffs.Add(entId, buff);
            if (RemoveAt.ContainsKey(entId))
            {
                RemoveAt[entId] = DateTime.Now.AddSeconds(secondsBetween + 5);
            }
            else
            {
                RemoveAt.Add(entId, DateTime.Now.AddSeconds(secondsBetween + 5));
            }
        }
        public static double GetRefinerySpeedMultiplier(long PlayerIdentityId, MyRefinery refin)
        {
            if (RemoveAt.TryGetValue(refin.EntityId, out var time))
            {
                if (DateTime.Now > time)
                {
                    SpeedBuffs.Remove(refin.EntityId);
                    return 1;
                }
            }
            if (SpeedBuffs.TryGetValue(refin.EntityId, out var buff))
            {
                return 1 + buff;
            }
            return 1;
        }

        public static double GetRefineryYieldMultiplier(long PlayerIdentityId, MyRefinery refin)
        {
            if (RemoveAt.TryGetValue(refin.EntityId, out var time))
            {
                if (DateTime.Now > time)
                {
                    SpeedBuffs.Remove(refin.EntityId);
                    return 1;
                }
            }
            if (YieldBuffs.TryGetValue(refin.EntityId, out var buff))
            {
                return 1 + buff;
            }
            return 1;
        }
        public static double GetAssemblerSpeedMultiplier(long PlayerId, MyAssembler Assembler)
        {
            if (RemoveAt.TryGetValue(Assembler.EntityId, out var time))
            {
                if (DateTime.Now > time)
                {
                    SpeedBuffs.Remove(Assembler.EntityId);
                    return 1;
                }
            }
            if (SpeedBuffs.TryGetValue(Assembler.EntityId, out var buff))
            {
                return 1 + buff;
            }
            return 1;
        }
    }
}
