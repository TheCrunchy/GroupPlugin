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
typeof(MySlimBlock).GetMethod("DoDamage", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(float), typeof(MyStringHash), typeof(bool), typeof(MyHitInfo?), typeof(long), typeof(long), typeof(bool) }, null) ??
throw new Exception("Failed to find patch method");
        internal static readonly MethodInfo patchSlimDamage =
        typeof(SlimBlockPatch).GetMethod(nameof(OnDamageRequest), BindingFlags.Static | BindingFlags.Public) ??
        throw new Exception("Failed to find patch method");
        public static void Patch(PatchContext ctx)
        {

            ctx.GetPattern(DamageRequest).Prefixes.Add(patchSlimDamage);
            AlliancePlugin.Log.Info("Patching slim block");
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

            message = new NotificationMessage("War is not enabled, or you need a faction.", 5000, "Red");
            //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
            ModCommunication.SendMessageTo(message, MySession.Static.Players.TryGetSteamId(attackerId));
            blockCooldowns.Remove(attackerId);
            blockCooldowns.Add(attackerId, DateTime.Now.AddSeconds(10));

        }

        private static Dictionary<long, DateTime> blockCooldowns = new Dictionary<long, DateTime>();
        public static Boolean Debug = true;
        public static Boolean OnDamageRequest(MySlimBlock __instance, float damage,
      MyStringHash damageType,
      bool sync,
      MyHitInfo? hitInfo,
      long attackerId, long realHitEntityId = 0, bool shouldDetonateAmmo = true)
        {
            //  MySlimBlock block = __instance;
            if (AlliancePlugin.config == null) return true;
            if (!AlliancePlugin.config.DisablePvP) return true;
            var loc = __instance.CubeGrid.PositionComp.GetPosition();
            if ((from territory in KamikazeTerritories.MessageHandler.Territories let distance = Vector3.Distance(loc, territory.Position) where distance <= territory.Radius select territory).Any())
            {
                return true;
            }

            if (damageType.ToString().Trim() == "Environment")
            {
                if (DateTime.Now < new DateTime(2022, 09, 1))
                {
                    damage = 0.0f;
                    return false;
                }
            }
            List<String> DamageTypesToIgnore = new List<string>()
            {
                "Deformation", "Suicide", "Temperature", "Asphyxia", "Environment"
            };
            long newattackerId = AlliancePlugin.GetAttacker(attackerId);
            if (DamageTypesToIgnore.Contains(damageType.ToString().Trim()))
            {
                return true;
            }
            if (newattackerId == 0L)
            {
                damage = 0.0f;
                //   AlliancePlugin.Log.Info("not 1");
                return false;
            }

            var owner = FacUtils.GetOwner(__instance.CubeGrid);
            if (newattackerId == owner)
            {
                return true;
            }
            if (owner == 0L)
            {
                damage = 0.0f;
                SendPvEMessage(newattackerId);
                //   AlliancePlugin.Log.Info("not 2");
                return false;
            }
            MyFaction attacker = MySession.Static.Factions.GetPlayerFaction(newattackerId) as MyFaction;

            MyFaction defender = MySession.Static.Factions.GetPlayerFaction(FacUtils.GetOwner(__instance.CubeGrid));
        

            if (defender != null && defender.Tag.Length > 3)
            {
                return true;
            }
            if (attacker == null || defender == null)
            {
                //  AlliancePlugin.Log.Info("not 3");
                SendPvEMessage(newattackerId);
                damage = 0.0f;
                return false;
            }

            if (!MySession.Static.Factions.AreFactionsEnemies(attacker.FactionId, defender.FactionId))
            {
                //   AlliancePlugin.Log.Info("not 4");
                SendPvEMessage(newattackerId);
                damage = 0.0f;
                return false;
            }
            else
            {
                //  AlliancePlugin.Log.Info("is 5");
                return true;
            }
        }


    }
}
