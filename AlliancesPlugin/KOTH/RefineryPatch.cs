using HarmonyLib;
using NLog;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;

namespace AlliancesPlugin
{
    [PatchShim]
    public static class RefineryPatch
    {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();



        internal static readonly MethodInfo update =
            typeof(MyRefinery).GetMethod("UpdateBeforeSimulation10", BindingFlags.Instance | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo updatePatch =
            typeof(RefineryPatch).GetMethod(nameof(TestPatchMethod), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");




        public static void Patch(PatchContext ctx)
        {

          //  ctx.GetPattern(update).Prefixes.Add(updatePatch);

        }

        public static List<long> RefineriesToUpdate = new List<long>();
        public static void TestPatchMethod(MyRefinery __instance)
        {
            if (__instance is MyRefinery refinery)
            {
                if (RefineriesToUpdate.Contains(refinery.EntityId))
                {
                    Dictionary<String, float> upgrades = refinery.UpgradeValues;
                    foreach (KeyValuePair<string, float> key in upgrades)
                    {
                        Log.Info(key.Key + " " + key.Value);

                    }
                    Log.Info(refinery.UpgradeValues["Productivity"]);
                    upgrades.TryGetValue("Productivity", out float speed);
                    refinery.AddUpgradeValue("Productivity", 5000f);
                    MyRefineryDefinition def = refinery.BlockDefinition as MyRefineryDefinition;
                 
                    Log.Info(refinery.UpgradeValues["Productivity"]);
                    RefineriesToUpdate.Remove(refinery.EntityId);
                    
                }
            }

        }
    }
}

