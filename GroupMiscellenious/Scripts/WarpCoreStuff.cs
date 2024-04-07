using System;
using System.Collections.Generic;
using System.Reflection;
using CrunchGroup;
using CrunchGroup.Territories;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using Torch.Managers.PatchManager;
using VRage.Game;

namespace GroupMiscellenious.Scripts
{
    [PatchShim]
    public static class WarpCoreStuff
    {


        internal static readonly MethodInfo enabledUpdate =
            typeof(MyFunctionalBlock).GetMethod("OnEnabledChanged", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo functionalBlockPatch =
            typeof(WarpCoreStuff).GetMethod(nameof(PatchTurningOn), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(enabledUpdate).Prefixes.Add(functionalBlockPatch);
        }
        public static bool PatchTurningOn(MyFunctionalBlock __instance)
        {
         
            if (__instance is IMyBeacon && !__instance.Enabled)
            {
                Core.Log.Info($"blocking a beacon from being turned off");
                __instance.Enabled = true;
                //do stuff here, return false to keep it turned on, return true to let them turn it off 
                return false;
            }

            return true;
        }
    }
}
