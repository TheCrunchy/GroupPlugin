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
        public static Boolean OnDamageRequest(MySlimBlock __instance, float damage,
      MyStringHash damageType,
      bool sync,
      MyHitInfo? hitInfo,
      long attackerId, long realHitEntityId = 0)
        {
            MySlimBlock block = __instance;
            if (AlliancePlugin.config != null)
            {
                if (AlliancePlugin.config.EnableAllianceSafeZones)
                {
                    long newattackerId = AlliancePlugin.GetAttacker(attackerId);
                    //    Log.Info(attackerId);
                   // Log.Info(info.Type.ToString());
                    //check if in zone
                    foreach (Territory ter in AlliancePlugin.Territories.Values)
                    {
                        if (ter.HasBigSafeZone && ter.Alliance != Guid.Empty)
                        {
                            //Log.Info("has zone");

                          
                                //    Log.Info("is an entity");
                                float distance = Vector3.Distance(block.CubeGrid.PositionComp.GetPosition(), new Vector3(ter.stationX, ter.stationY, ter.stationZ));
                                //    Log.Info(distance);
                                if (distance <= ter.SafeZoneRadiusFromStationCoords)
                                {
                                    //     Log.Info("in distance");
                                    if (!damageType.Equals("Grind"))
                                    {
                                        //        Log.Info("Denying damage");
                                        damage = 0.0f;
                                        return false;
                                    }
                                    //if in zone and damage type is from weapons/ramming deny it
                                    if (newattackerId == 0L)
                                    {
                                        //      Log.Info("Denying damage");
                                        damage = 0.0f;
                                        return false;
                                    }
                                    MyFaction attacker = MySession.Static.Factions.GetPlayerFaction(newattackerId) as MyFaction;
                                    if (attacker != null)
                                    {
                                        Alliance alliance = AlliancePlugin.GetAllianceNoLoading(attacker);
                                        if (alliance != null && ter.Alliance == alliance.AllianceId)
                                        {

                                            if (!alliance.HasAccess(MySession.Static.Players.TryGetSteamId(newattackerId), AccessLevel.GrindInSafeZone))
                                            {
                                                damage = 0.0f;
                                                return false;
                                            }
                                            else
                                            {
                                                return true;
                                            }
                                        }
                                        else
                                        {
                                            damage = 0.0f;
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        damage = 0.0f;
                                        return false;
                                    }
                                }
                            
                        }
                    }
                }
            }
            return true;
        }


    }
}
