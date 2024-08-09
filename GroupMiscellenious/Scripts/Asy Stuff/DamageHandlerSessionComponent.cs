using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup;
using CrunchGroup.Extensions;
using CrunchGroup.Handlers;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using SpaceEngineers.Game.Weapons.Guns;
using Torch.Managers.PatchManager;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace GroupMiscellenious.Scripts.Asy_Stuff
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
        }

 

        private static Dictionary<long, DateTime> blockCooldowns = new Dictionary<long, DateTime>();
        public static Boolean Debug = true;
        public static Boolean OnDamageRequest(MySlimBlock __instance, ref float damage,
      MyStringHash damageType,
      bool sync,
      MyHitInfo? hitInfo,
      long attackerId, long realHitEntityId = 0, bool shouldDetonateAmmo = true)
        {
            long newattackerId = DamageHandlerSessionComponent.GetAttacker(attackerId);

            //  MySlimBlock block = __instance;
            Core.Log.Info("2");
            var attackersFaction = MySession.Static.Factions.GetPlayerFaction(newattackerId);
            if (attackersFaction == null)
            {
                return true;
            }
            Core.Log.Info("3");
            var attackersGroup = GroupHandler.GetFactionsGroup(attackersFaction.FactionId);
            if (attackersGroup == null)
            {
                return true;
            }
            Core.Log.Info("4");
            var owner = __instance.CubeGrid.GetGridOwnerFaction();

            if (owner == null)
            {
                return true;
            }
            Core.Log.Info("5");
            var groupPartOf = GroupHandler.LoadedGroups.FirstOrDefault(x => x.Value.GroupOwnedGridsNPCTag == owner.Tag);
            if (groupPartOf.Value == null)
            {

                return true;
            }
            if (groupPartOf.Key == attackersGroup.GroupId)
            {
                Core.Log.Info("6");
                damage = 0.0f;
                return false;
            }

            return true;
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class DamageHandlerSessionComponent : MySessionComponentBase
    {
        internal static bool Update => _tick % 20 == 0;

        private static int _tick;

        public override void UpdateBeforeSimulation()
        {

            base.UpdateBeforeSimulation();
            if (_tick == 0)
            {
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(100, DamageHandler);
            }
            UpdateTick();
            
            if (!Update)
                return;
        }

        private static void UpdateTick() => _tick++;

        private void DamageHandler(object target, ref MyDamageInformation info)
        {
            //well this was a waste of time but i already made it so fuck it 
            var attackerId = GetAttacker(info.AttackerId);
            Core.Log.Info($"{info.Type}");
            if (!(target is MySlimBlock block))
            {
                //handle characters differently
                return;
            };
            Core.Log.Info("2");
            var attackersFaction = MySession.Static.Factions.GetPlayerFaction(attackerId);
            if (attackersFaction == null)
            {
                return;
            }
            Core.Log.Info("3");
            var attackersGroup = GroupHandler.GetFactionsGroup(attackersFaction.FactionId);
            if (attackersGroup == null)
            {
                return;
            }
            Core.Log.Info("4");
            var owner = block.CubeGrid.GetGridOwnerFaction();

            if (owner == null)
            {
                return;
            }
            Core.Log.Info("5");
            var groupPartOf = GroupHandler.LoadedGroups.FirstOrDefault(x => x.Value.GroupOwnedGridsNPCTag == owner.Tag);
            if (groupPartOf.Value == null)
            {
       
                return;
            }
            if (groupPartOf.Key == attackersGroup.GroupId)
            {
                Core.Log.Info("6");
                info.Amount = 0.0f;
            }

        }

        public static long GetAttacker(long attackerId)
        {

            var entity = MyAPIGateway.Entities.GetEntityById(attackerId);
            Core.Log.Info($"{entity.GetType()}");
            if (entity == null)
                return 0L;
            
            if (entity is MyPlanet)
            {

                return 0L;
            }

            switch (entity)
            {
                case MyCharacter character:
                    Core.Log.Info("character");
                    return character.GetPlayerIdentityId();
                case IMyEngineerToolBase toolbase:
                    Core.Log.Info("tool");
                    return toolbase.OwnerIdentityId;
                case MyLargeTurretBase turret:
                    Core.Log.Info("turret");
                    return turret.OwnerId;
                case MyShipToolBase shipTool:
                    Core.Log.Info("ship tool");
                    return shipTool.OwnerId;
                case IMyGunBaseUser gunUser:
                    Core.Log.Info("gun");
                    return gunUser.OwnerId;
                case MyFunctionalBlock block:
                    Core.Log.Info("block");
                    return block.OwnerId;
                case MyCubeGrid grid:
                    Core.Log.Info("grid");
                    return grid.GetGridOwner();
                default:
                    return 0L;
            }
        }
    }
}
