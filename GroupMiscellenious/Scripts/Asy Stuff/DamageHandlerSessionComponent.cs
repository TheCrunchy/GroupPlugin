using System;
using System.Collections.Generic;
using System.Linq;
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
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace GroupMiscellenious.Scripts.Asy_Stuff
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class DamageHandlerSessionComponent : MySessionComponentBase
    {
        internal static bool Update => _tick % 20 == 0;

        private static int _tick;

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            UpdateTick();
            
            if (!Update)
                return;
          
        }

        private static void UpdateTick() => _tick++;

        public override void LoadData()
        {
            Core.Log.Info("Adding damage handler");
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageHandler);
        }

        private void DamageHandler(object target, ref MyDamageInformation info)
        {
            var attackerId = GetAttacker(info.AttackerId);
            if (!(target is MySlimBlock block))
            {
                //handle characters differently
                return;
            };

            var attackersFaction = MySession.Static.Factions.GetPlayerFaction(attackerId);
            if (attackersFaction == null)
            {
                return;
            }

            var attackersGroup = GroupHandler.GetFactionsGroup(attackersFaction.FactionId);
            if (attackersGroup == null)
            {
                return;
            }

            var owner = block.CubeGrid.GetGridOwnerFaction();

            if (owner == null)
            {
                return;
            }

            var groupPartOf = GroupHandler.LoadedGroups.FirstOrDefault(x => x.Value.GroupOwnedGridsNPCTag == owner.Tag);
            if (groupPartOf.Value == null)
            {
                return;
            }

            if (groupPartOf.Key == attackersGroup.GroupId)
            {
                info.Amount = 0.0f;
            }

        }

        public static long GetAttacker(long attackerId)
        {

            var entity = MyAPIGateway.Entities.GetEntityById(attackerId);

            if (entity == null)
                return 0L;

            if (entity is MyPlanet)
            {

                return 0L;
            }

            if (entity is MyCharacter character)
            {

                return character.GetPlayerIdentityId();
            }

            if (entity is IMyEngineerToolBase toolbase)
            {

                return toolbase.OwnerIdentityId;

            }

            if (entity is MyLargeTurretBase turret)
            {

                return turret.OwnerId;

            }

            if (entity is MyShipToolBase shipTool)
            {

                return shipTool.OwnerId;
            }


            if (entity is IMyGunBaseUser gunUser)
            {

                return gunUser.OwnerId;

            }

            if (entity is MyFunctionalBlock block)
            {

                return block.OwnerId;
            }

            if (entity is MyCubeGrid grid)
            {
                return grid.GetGridOwner();
            }

            return 0L;
        }
    }
}
