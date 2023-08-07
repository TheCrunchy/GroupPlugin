using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities.Cube;
using Torch.Managers.PatchManager;
using VRage.Game.ObjectBuilders;

namespace AlliancesPlugin.Territory_Version_2
{
    [PatchShim]
    public static class FunctionalBlockPatch
    {

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

        public static Boolean IsDisabled(long blockEntityId)
        {
            if (!BlocksDisabled.TryGetValue(blockEntityId, out var time)) return false;
            if (DateTime.Now < time)
            {
                return true;
            }

            BlocksDisabled.Remove(blockEntityId);

            return false;
        }

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(update).Prefixes.Add(updatePatch);
            ctx.GetPattern(update2).Prefixes.Add(updatePatch);
            ctx.GetPattern(enabledUpdate).Prefixes.Add(functionalBlockPatch);
        }

        public static bool PatchTurningOn(MyFunctionalBlock __instance)
        {
            return !IsDisabled(__instance.EntityId);
        }

        public static Boolean KeepDisabled(MyFunctionalBlock __instance)
        {
            if (IsDisabled(__instance.EntityId))
            {
                __instance.Enabled = false;
            }
            return false;
        }
    }
}
