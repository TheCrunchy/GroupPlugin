﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Torch.Managers.PatchManager;

namespace Territory.Territory_Version_2
{
    public static class SkoTweaksPatch
    {
        internal static readonly MethodInfo HandleMessagePatch = typeof(SkoTweaksPatch).GetMethod(nameof(HandleMessage), BindingFlags.Static | BindingFlags.Public) ??
                                                                 throw new Exception("Failed to find patch method");
        public static void Patch(PatchContext ctx)
        {
            var HandleMessageMethod = TerritoryPlugin.SKO.GetType().Assembly.GetType("SKO.Torch.Plugins.Tweaks.Modules.SafeZoneModule").GetMethod("IsEmptySafeZone", BindingFlags.Instance | BindingFlags.NonPublic);
            if (HandleMessageMethod == null)
            {
                TerritoryPlugin.Log.Info("SKO NULL");
                return;
            }

            TerritoryPlugin.Log.Info("Patched SKO");
            ctx.GetPattern(HandleMessageMethod).Suffixes.Add(HandleMessagePatch);
        }

        public static void HandleMessage(MySafeZone safeZone, bool __result)
        {
            if (CaptureHandler.TrackedSafeZoneIds.Contains(safeZone.EntityId))
            {
                __result = false;
            }
        }
    }
}