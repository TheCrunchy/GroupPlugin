using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;

namespace CrunchGroup.Territories.Statics
{
    [PatchShim]
    public static class AssemblerPatch
    {
        internal static readonly MethodInfo update =
            typeof(MyAssembler).GetMethod("CalculateBlueprintProductionTime", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        public static Dictionary<long, float> AssemblerSpeeds = new Dictionary<long, float>();

        public static void Patch(PatchContext ctx)
        {
            var harmony = new Harmony("Crunch.Assembler.Patch");
            harmony.PatchAll();
        }

        private static float GetBuff(long PlayerId, MyAssembler Assembler)
        {
            if (AssemblerSpeeds.TryGetValue(Assembler.EntityId, out var speedBuff))
            {
                return speedBuff;
            }
            //get the buff if an assembler is in this list
            double buff = 1;

            return (float)(buff);
        }

        public static float PatchMethod(MyAssembler __instance, MyBlueprintDefinitionBase currentBlueprint)
        {
            var buff = GetBuff(__instance.OwnerId, __instance);

            var speed = (double)(((MyAssemblerDefinition)__instance.BlockDefinition).AssemblySpeed + (double)__instance.UpgradeValues["Productivity"]) * buff;
            return (float)Math.Round((double)currentBlueprint.BaseProductionTimeInSeconds * 1000.0 / ((double)MySession.Static.AssemblerSpeedMultiplier * speed));
        }

        [HarmonyPatch(typeof(MyAssembler))]
        [HarmonyPatch("UpdateProduction")]
        public static class HarmonyTranspilePatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var replaceMethod = typeof(AssemblerPatch).GetMethod(nameof(PatchMethod));
                var codes = new List<CodeInstruction>(instructions);
                return codes.MethodReplacer(update, replaceMethod);
            }
        }

    }
}