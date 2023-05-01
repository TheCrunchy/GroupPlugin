using System;
using System.Collections.Generic;
using System.Linq;
using AlliancesPlugin.JumpGates;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch;
using VRageMath;

namespace AlliancesPlugin.Alliances.Gates
{
    public static class NewGateLogic
    {
        public static void DoGateLogic()
        {
            var players = MySession.Static.Players.GetOnlinePlayers();
            if (AlliancePlugin.AllGates == null)
            {
                return;
            }
            foreach (var gate in AlliancePlugin.AllGates.Values)
            {
                if (!gate.Enabled)
                    continue;

                if (!gate.CanJumpFrom)
                    continue;

                if (gate.TargetGateId == gate.GateId)
                    continue;
                if (!AlliancePlugin.AllGates.ContainsKey(gate.TargetGateId))
                    continue;

                var target = AlliancePlugin.AllGates[gate.TargetGateId];
                if (!target.Enabled || target == null)
                    continue;
                if (target.TargetGateId == target.GateId)
                    continue;

                if (!gate.WorldName.Equals(MyMultiplayer.Static.HostName))
                    continue;
                
                if (gate.RequirePilot)
                {
                    foreach (var player in players)
                    {
                        if (!(player?.Controller?.ControlledEntity is MyCockpit controller)) continue;

                        var Distance = Vector3.Distance(gate.Position, controller.PositionComp.GetPosition());
                        if (Distance <= gate.RadiusToJump)
                        {
                            if (!AlliancePlugin.DoFeeStuff(player, gate, controller.CubeGrid))
                                continue;
                            var rand = new Random();
                            var offset = new Vector3(rand.Next(AlliancePlugin.config.JumpGateMinimumOffset, AlliancePlugin.config.JumPGateMaximumOffset), rand.Next(AlliancePlugin.config.JumpGateMinimumOffset, AlliancePlugin.config.JumPGateMaximumOffset), rand.Next(AlliancePlugin.config.JumpGateMinimumOffset, AlliancePlugin.config.JumPGateMaximumOffset));
                            var newPos = new Vector3D(target.Position + offset);
                            var newPosition = MyEntities.FindFreePlace(newPos, (float)GridManager.FindBoundingSphere(controller.CubeGrid).Radius);
                            if (newPosition.Value == null)
                            {
                                break;
                            }
                            var worldMatrix = MatrixD.CreateWorld(newPosition.Value, controller.CubeGrid.WorldMatrix.Forward, controller.CubeGrid.WorldMatrix.Up);
                            controller.CubeGrid.Teleport(worldMatrix);
                            AlliancePlugin.Log.Info("Gate travel " + gate.GateName + " for " + player.DisplayName + " in " + controller.CubeGrid.DisplayName);
                        }
                        else
                        {
                            if (gate.fee > 0 && Distance <= 500)
                            {
                                AlliancePlugin.DoFeeMessage(player, gate, Distance);
                            }
                            else
                            {


                                if (Distance <= 500)
                                {
                                    AlliancePlugin.SendPlayerNotify(player, 1000, "You will jump in " + Distance + " meters", "Green");
                                }
                            }
                        }
                    }
                }
                else
                {
                    var sphere = new BoundingSphereD(gate.Position, 600);
                    var entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>();
                    foreach (var player in players)
                    {
                        if (player?.Controller?.ControlledEntity is MyCockpit controller)
                        {
                            DoGridTravel(gate, controller.CubeGrid, player);
                            continue;
                        }
//     AlliancePlugin.Log.Info("1");
                        if (player.Character == null)
                        {
                            continue;
                        }
                        var Distance = Vector3.Distance(gate.Position, player.Character.PositionComp.GetPosition());
                        if (Distance <= gate.RadiusToJump)
                        {
                            if (!AlliancePlugin.DoFeeStuff(player, gate, null))
                                continue;

                            var rand = new Random();
                            var offset = new Vector3(rand.Next(AlliancePlugin.config.JumpGateMinimumOffset, AlliancePlugin.config.JumPGateMaximumOffset), rand.Next(AlliancePlugin.config.JumpGateMinimumOffset, AlliancePlugin.config.JumPGateMaximumOffset), rand.Next(AlliancePlugin.config.JumpGateMinimumOffset, AlliancePlugin.config.JumPGateMaximumOffset));
                            var newPos = new Vector3D(target.Position + offset);
                            var newPosition = MyEntities.FindFreePlace(newPos, 50);
                            if (newPosition.Value == null)
                            {
                                break;
                            }
                            var worldMatrix = MatrixD.CreateWorld(newPosition.Value, player.Character.WorldMatrix.Forward, player.Character.WorldMatrix.Up);
                            player.Character.Teleport(worldMatrix);
                            AlliancePlugin.Log.Info("Gate travel " + gate.GateName + " for " + player.DisplayName + " in suit");
                        }
                        else
                        {
                            if (gate.fee > 0 && Distance <= 500)
                            {
                                AlliancePlugin.DoFeeMessage(player, gate, Distance);
                            }
                            else
                            {
                                if (Distance <= 500)
                                {
                                    AlliancePlugin.SendPlayerNotify(player, 1000, "You will jump in " + Distance + " meters", "Green");
                                }
                            }
                        }
                    }
                    foreach (MyCubeGrid grid in entities.Where(x => x is MyCubeGrid grid))
                    {
                        var owner = FacUtils.GetOwner(grid);
                        var steamId = MySession.Static.Players.TryGetSteamId(owner);
                        var player = MySession.Static.Players.TryGetPlayerBySteamId(steamId);
                        if (player == null)
                            continue;
                        DoGridTravel(gate,grid, player);
                    }
                }
            }
        }
        public static void DoGridTravel(JumpGate gate, MyCubeGrid grid, MyPlayer player)
        {
            if (grid.IsStatic)
                return;

            var Distance = Vector3.Distance(gate.Position, grid.PositionComp.GetPosition());
     
            if (Distance <= gate.RadiusToJump)
            {

                if (!AlliancePlugin.DoFeeStuff(player, gate, grid))
                    return;

                var target = AlliancePlugin.AllGates[gate.TargetGateId];
                var rand = new Random();
                var offset = new Vector3(rand.Next(AlliancePlugin.config.JumpGateMinimumOffset, AlliancePlugin.config.JumPGateMaximumOffset), rand.Next(AlliancePlugin.config.JumpGateMinimumOffset, AlliancePlugin.config.JumPGateMaximumOffset), rand.Next(AlliancePlugin.config.JumpGateMinimumOffset, AlliancePlugin.config.JumPGateMaximumOffset));
                var newPos = new Vector3D(target.Position + offset);
                var newPosition = MyEntities.FindFreePlace(newPos, (float)GridManager.FindBoundingSphere(grid).Radius);
                if (newPosition.Value == null)
                {
                    return;
                }
                var worldMatrix = MatrixD.CreateWorld(newPosition.Value, grid.WorldMatrix.Forward, grid.WorldMatrix.Up);
                grid.Teleport(worldMatrix);
                AlliancePlugin.Log.Info("Gate travel " + gate.GateName + " for " + player.DisplayName + " in " + grid.DisplayName);
            }
            else
            {
                if (gate.fee > 0 && Distance <= 500)
                {
                    AlliancePlugin.DoFeeMessage(player, gate, Distance);
                }
                else
                {
                    if (Distance <= 500)
                    {
                        AlliancePlugin.SendPlayerNotify(player, 1000, "You will jump in " + Distance + " meters", "Green");
                    }
                }
            }
        }
    }
}
