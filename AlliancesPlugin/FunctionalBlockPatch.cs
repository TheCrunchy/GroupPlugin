using System;
using System.Collections.Generic;
using System.Reflection;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using VRage.Game;

namespace AlliancesPlugin.KOTH
{
    [PatchShim]
    public class FunctionalBlockPatch
    {

        internal static readonly MethodInfo update =
        typeof(MyFunctionalBlock).GetMethod("UpdateBeforeSimulation10", BindingFlags.Instance | BindingFlags.Public) ??
        throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo update2 =
        typeof(MyFunctionalBlock).GetMethod("UpdateBeforeSimulation100", BindingFlags.Instance | BindingFlags.Public) ??
        throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo storePatch =
       typeof(FunctionalBlockPatch).GetMethod(nameof(Transfer), BindingFlags.Static | BindingFlags.Public) ??
       throw new Exception("Failed to find patch method");
        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(update).Prefixes.Add(storePatch);
            ctx.GetPattern(update2).Prefixes.Add(storePatch);
        }
        public static Dictionary<long, ulong> transferList = new Dictionary<long, ulong>();
        public static List<long> DisableThese = new List<long>();
        public static void Transfer(MyFunctionalBlock __instance) {
            if (DisableThese.Contains(__instance.EntityId)){
                __instance.Enabled = false;
            }
            if (transferList.TryGetValue(__instance.EntityId, out ulong steamid))
            {
                long id = MySession.Static.Players.TryGetIdentityId(steamid);
               __instance.ChangeOwner(id, MyOwnershipShareModeEnum.None);
                transferList.Remove(__instance.EntityId);
            }
        }

    }
}
