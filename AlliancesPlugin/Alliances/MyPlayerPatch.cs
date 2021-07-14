﻿using HarmonyLib;
using NLog;
using NLog.Config;
using NLog.Targets;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.Gui;
using VRage.Game.ModAPI;
using VRageMath;
using static HarmonyLib.AccessTools;

namespace AlliancesPlugin
{
    [PatchShim]
    public static class MyPlayerPatch
    {
        public static void DoPatching()
        {
            var harmony = new Harmony("Crunch.Player.Patch");
            harmony.PatchAll();
            AlliancePlugin.Log.Info("Patching the sensor shit?");
        }
        public static void Patch(PatchContext ctx)
        {
            DoPatching();
        }
        //[HarmonyPatch(typeof(MyBeacon))]
        //[HarmonyPatch("GetHudParams")]
        //class BeaconPatch
        //{
        //    static void Postfix(bool allowBlink, ref List<MyHudEntityParams> __result, MyBeacon __instance)
        //    {
        //        AlliancePlugin.Log.Info("beacon");
        //        if (__instance is MyBeacon beacon)
        //        {
        //            MyRadioBroadcaster broadcaster = (MyRadioBroadcaster) beacon.Components.Get<MyDataBroadcaster>();
                 
        //            if (beacon.IsWorking)
        //            {
        //                List<MyHudEntityParams> hudParams = __result;
        //                StringBuilder hudText = new StringBuilder();
        //                hudText.Append("DAVE");
        //                if (hudText.Length > 0)
        //                {
        //                    StringBuilder text = hudParams[0].Text;
        //                    text.Clear();
        //                    text.Append("BOB");
        //                    text.Append(".");
        //                    text.Append((object)hudText);
        //                }
        //                __result = hudParams;
        //            }

        //            return;
        //            // make sure you only skip if really necessary
        //        }
        //    }
        //}
        [HarmonyPatch(typeof(MyPlayer))]
        [HarmonyPatch("GetRelationBetweenPlayers")]
        class PlayerPatch
        {
            static void Postfix(long playerId1,
      long playerId2, ref MyRelationsBetweenPlayerAndBlock __result)
            {
                if (playerId1 == playerId2)
                {
                    return;
                }
                IMyFaction playerFaction1 = MySession.Static.Factions.TryGetPlayerFaction(playerId1);
                IMyFaction playerFaction2 = MySession.Static.Factions.TryGetPlayerFaction(playerId2);
                if (playerFaction1 != null && playerFaction2 != null)
                {

                    if (AlliancePlugin.GetAllianceNoLoading(playerFaction1 as MyFaction) != null && AlliancePlugin.GetAllianceNoLoading(playerFaction2 as MyFaction) != null)
                    {

                        if (AlliancePlugin.GetAllianceNoLoading(playerFaction1 as MyFaction) == AlliancePlugin.GetAllianceNoLoading(playerFaction2 as MyFaction))
                        {
                            //    AlliancePlugin.Log.Info("Same alliance?");
                            __result = MyRelationsBetweenPlayerAndBlock.Friends;
                            return;
                        }
                    }
                    if (MySession.Static.Factions.AreFactionsFriends(playerFaction1.FactionId, playerFaction2.FactionId))
                    {
                       __result = MyRelationsBetweenPlayerAndBlock.Friends;
                        return;
                    }
                }

                return;
                // make sure you only skip if really necessary
            }
        }




    }
}
