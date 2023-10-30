using System;
using System.Collections.Generic;
using System.Reflection;
using Sandbox.Game.Entities.Cube;
using Torch.Managers.PatchManager;
using VRage.Game;

namespace Territory.Territories
{
    [PatchShim]
    public static class FunctionalBlockPatch
    {

        internal static readonly MethodInfo update1 =
            typeof(MyFunctionalBlock).GetMethod("UpdateBeforeSimulation",
                BindingFlags.Instance | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");
        internal static readonly MethodInfo update =
            typeof(MyFunctionalBlock).GetMethod("UpdateBeforeSimulation10",
                BindingFlags.Instance | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo update2 =
            typeof(MyFunctionalBlock).GetMethod("UpdateBeforeSimulation100",
                BindingFlags.Instance | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo updatePatch =
            typeof(FunctionalBlockPatch).GetMethod(nameof(KeepDisabled), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo enabledUpdate =
            typeof(MyFunctionalBlock).GetMethod("OnEnabledChanged", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo functionalBlockPatch =
            typeof(FunctionalBlockPatch).GetMethod(nameof(PatchTurningOn), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        public static Dictionary<long, DateTime> BlocksDisabled = new Dictionary<long, DateTime>();

        public static void AddBlockToDisable(long blockEntityId, int secondsToDisable)
        {
            if (BlocksDisabled.ContainsKey(blockEntityId))
            {
                BlocksDisabled[blockEntityId] = DateTime.Now.AddSeconds(secondsToDisable);
            }
            else
            {
                BlocksDisabled.Add(blockEntityId, DateTime.Now.AddSeconds(secondsToDisable));
            }
        }
        public static void AddBlockToDamage(long blockEntityId, float damage)
        {
            if (DamageThese.ContainsKey(blockEntityId))
            {
                DamageThese[blockEntityId] += damage;
            }
            else
            {
                DamageThese.Add(blockEntityId, damage);
            }
        }

        public static Boolean IsDisabled(long blockEntityId)
        {
            if (!BlocksDisabled.TryGetValue(blockEntityId, out var time)) return false;
            if (DateTime.Now < time)
            {
                return true;
            }

            DeleteCount.Remove(blockEntityId);
            BlocksDisabled.Remove(blockEntityId);

            return false;
        }

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(update1).Prefixes.Add(updatePatch);
            ctx.GetPattern(update).Prefixes.Add(updatePatch);
            ctx.GetPattern(update2).Prefixes.Add(updatePatch);
            ctx.GetPattern(enabledUpdate).Prefixes.Add(functionalBlockPatch);
        }

        public static Dictionary<long, float> DamageThese = new Dictionary<long, float>();
        public static Dictionary<long, int> DeleteCount = new Dictionary<long, int>();
        public static bool PatchTurningOn(MyFunctionalBlock __instance)
        {
            if (IsDisabled(__instance.EntityId))
            {
                if (DeleteCount.ContainsKey(__instance.EntityId))
                {
                    DeleteCount[__instance.EntityId] += 1;
                }
                else
                {
                    DeleteCount.Add(__instance.EntityId, 1);
                }
                //   TerritoryPlugin.Log.Info("Disabling");
                __instance.Enabled = false;
                return false;
            }

            return true;
        }

        public static Boolean KeepDisabled(MyFunctionalBlock __instance)
        {
            if (DamageThese.TryGetValue(__instance.EntityId, out var damage))
            {

                __instance.SlimBlock.DoDamage(damage, MyDamageType.Fire);
                __instance.SlimBlock.UpdateVisual(true);
                DamageThese.Remove(__instance.EntityId);
            }
            if (DeleteCount.ContainsKey(__instance.EntityId))
            {
                if (DeleteCount[__instance.EntityId] >= 10)
                {
                    __instance.Enabled = false;
                    __instance.SlimBlock.DoDamage(__instance.SlimBlock.MaxIntegrity * 50, MyDamageType.Fire);
                    return false;
                }
            }
            if (IsDisabled(__instance.EntityId))
            {
                __instance.Enabled = false;
                return false;
            }

            return true;
        }
    }
}
