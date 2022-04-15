using AlliancesPlugin.KOTH;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;
using VRage.Game;
using VRageMath;

namespace AlliancesPlugin.NewTerritories
{
    [PatchShim]
    public class SafezoneBlockPatch
    {
        internal static readonly MethodInfo update2 =
         typeof(MySafeZoneComponent).GetMethod("StartActivationCountdown", BindingFlags.Instance | BindingFlags.NonPublic) ??
         throw new Exception("Failed to find patch method");



        internal static readonly MethodInfo safezonePatch =
            typeof(SafezoneBlockPatch).GetMethod(nameof(SafezoneBlockPatchMethod), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(update2).Prefixes.Add(safezonePatch);
        }
        public class TaxItem
        {
            public long playerId;
            public long price;
            public Guid territory;
        }

        public static void SafezoneBlockPatchMethod(MySafeZoneComponent __instance)
        {
            AlliancePlugin.Log.Info("SAFE ZONE INIT");
            if (__instance != null)
            {
                AlliancePlugin.Log.Info("SAFE ZONE ISNT NULL");
            }
        }

    }
}
