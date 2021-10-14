using AlliancesPlugin.Alliances;
using HarmonyLib;
using NLog;
using NLog.Config;
using NLog.Targets;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.World;
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

        //            List<MyHudEntityParams> hudParams = __result;
        //            StringBuilder hudText = beacon.HudText;
        //            if (hudText.Length > 0)
        //            {
        //                StringBuilder text = hudParams[0].Text;
        //                text.Clear();
        //                if (!string.IsNullOrEmpty(__instance.GetOwnerFactionTag()))
        //                {
        //                    text.Append(__instance.GetOwnerFactionTag());
        //                    text.Append(".....");
        //                }
        //                text.Append((object)hudText);
        //            }

        //            //   .AddRange((IEnumerable<MyHudEntityParams>)hudParams);
        //            List<MyHudEntityParams> hud = (List<MyHudEntityParams>)Traverse.Create(beacon).Field("m_hudParams").GetValue();
        //            hud.Clear();
        //            hud.AddRange((IEnumerable<MyHudEntityParams>)hudParams);
        //            Traverse.Create(beacon).Field("m_hudParams").SetValue(hud);
        //            return;
        //            // make sure you only skip if really necessary
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(MySessionComponentEconomy))]
        //[HarmonyPatch("GetStoreCreationLimitPerPlayer")]
        //class ContractPatch
        //{
        //    static void Postfix(ref int __result)
        //    {
        //        __result = 50000;
        //    }
        //}


        //[HarmonyPatch(typeof(MyContractBlock))]
        //[HarmonyPatch("AcceptContract")]
        //[HarmonyPatch(new Type[] { typeof(long), typeof(long)})]
        //class ContractPatch
        //{
        //   static void Postfix(long identityId, long contractId)
        //    {
        //        MySessionComponentContractSystem component = MySession.Static.GetComponent<MySessionComponentContractSystem>();
        //        MyDefinitionId? id = component.GetContractDefinitionId(contractId);

        //        if (id != null)
        //        {
        //            AlliancePlugin.Log.Info(id.ToString());
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


        [HarmonyPatch(typeof(MySpaceRespawnComponent))]
        [HarmonyPatch("RespawnRequest_Implementation")]
        class RespawnPatchExistingBody
        {
            static void Postfix(ulong steamPlayerId, int serialId)
            {
              
             //   AlliancePlugin.Log.Info("DGSDGSDG");
                if (AlliancePlugin.UpdateThese.ContainsKey(steamPlayerId))
                {
                    AlliancePlugin.UpdateThese[steamPlayerId] = DateTime.Now.AddSeconds(5);
                }
                else
                {
                    AlliancePlugin.UpdateThese.Add(steamPlayerId,DateTime.Now.AddSeconds(5));
                }
            
                    return;
                // make sure you only skip if really necessary
            }
        }
    }
}
