using AlliancesPlugin.Alliances;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace AlliancesPlugin.KOTH
{
    [PatchShim]
    public static class SlimBlockPatch
    {
        internal static readonly MethodInfo DamageRequest =
typeof(MySlimBlock).GetMethod("DoDamage", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(float), typeof(MyStringHash), typeof(bool), typeof(MyHitInfo?), typeof(long), typeof(long) }, null) ??
throw new Exception("Failed to find patch method");
        internal static readonly MethodInfo patchSlimDamage =
        typeof(SlimBlockPatch).GetMethod(nameof(OnDamageRequest), BindingFlags.Static | BindingFlags.Public) ??
        throw new Exception("Failed to find patch method");
        public static void Patch(PatchContext ctx)
        {

            ctx.GetPattern(DamageRequest).Prefixes.Add(patchSlimDamage);
        }

        public static void SendPvEMessage(long attackerId)
        {
            if (blockCooldowns.TryGetValue(attackerId, out DateTime time))
            {
                if (DateTime.Now < time)
                {

                    return;
                }
            }

            NotificationMessage message;
         
            message = new NotificationMessage("PvP Is not enabled.", 5000, "Red");
            //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
            ModCommunication.SendMessageTo(message, MySession.Static.Players.TryGetSteamId(attackerId));
            blockCooldowns.Remove(attackerId);
            blockCooldowns.Add(attackerId, DateTime.Now.AddSeconds(10));

        }

        private static Dictionary<long, DateTime> blockCooldowns = new Dictionary<long, DateTime>();

        public static Boolean OnDamageRequest(MySlimBlock __instance, float damage,
      MyStringHash damageType,
      bool sync,
      MyHitInfo? hitInfo,
      long attackerId, long realHitEntityId = 0)
        {
            MySlimBlock block = __instance;
            if (AlliancePlugin.config != null)
            {
                if (AlliancePlugin.config.DisablePvP)
                {
                    long newattackerId = AlliancePlugin.GetAttacker(attackerId);
              

                    if (__instance.OwnerId == newattackerId)
                    {
                        return true;
                    }
                    //    Log.Info(attackerId);
                    // Log.Info(info.Type.ToString());
                    //check if in zone

                    //Log.Info("has zone");


                    //    Log.Info("is an entity");

                    //     Log.Info("in distance");
                    if (!damageType.Equals("Grind"))
                    {
                        //        Log.Info("Denying damage");
                        damage = 0.0f;
                        return false;
                    }
                    //if in zone and damage type is from weapons/ramming deny it
                  

                    Boolean DenyDamage = false;
                    if (newattackerId == 0L)
                    {
                        //      Log.Info("Denying damage");
                        DenyDamage = true;
                    }

                    MyFaction attacker = MySession.Static.Factions.GetPlayerFaction(newattackerId) as MyFaction;
                    MyFaction defender = MySession.Static.Factions.GetPlayerFaction(FacUtils.GetOwner(__instance.CubeGrid));
                    if (attacker != null)
                    {
                        if (defender != null)
                        {
                            if (attacker.FactionId == defender.FactionId)
                            {
                                return true;
                            }
                        }
                        SendPvEMessage(newattackerId);
                        damage = 0.0f;
                        return false;
                    }
                    else
                    {
                        SendPvEMessage(newattackerId);
                        damage = 0.0f;
                        //send message about PvE
                        return false;
                    }


                }
            }
            return true;
        }


    }
}
