using NLog;
using NLog.Config;
using NLog.Targets;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
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
using VRageMath;

namespace AlliancesPlugin
{
    [PatchShim]
    public static class JumpPatch
    {
        public static List<JumpZone> Zones = new List<JumpZone>();


        public static Logger Log = LogManager.GetLogger("JumpLog");
        public static void ApplyLogging()
        {

            var rules = LogManager.Configuration.LoggingRules;

            for (int i = rules.Count - 1; i >= 0; i--)
            {

                var rule = rules[i];

                if (rule.LoggerNamePattern == "JumpLog")
                    rules.RemoveAt(i);
            }



            var logTarget = new FileTarget
            {
                FileName = "Logs/JumpLog-" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + ".txt",
                Layout = "${var:logStamp} ${var:logContent}"
            };

            var logRule = new LoggingRule("JumpLog", LogLevel.Debug, logTarget)
            {
                Final = true
            };

            rules.Insert(0, logRule);

            LogManager.Configuration.Reload();
        }


        internal static readonly MethodInfo RequestJump =
    typeof(MyGridJumpDriveSystem).GetMethod("OnJumpRequested", BindingFlags.Static | BindingFlags.NonPublic) ??
    throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo DenyJumpPatch =
            typeof(JumpPatch).GetMethod(nameof(PatchRequestJump), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");


        public static void Patch(PatchContext ctx)
        {

            ctx.GetPattern(RequestJump).Prefixes.Add(DenyJumpPatch);
            Log.Info("Patching Successful jump drive stuff");
            ApplyLogging();
        }




        public static bool PatchRequestJump(long entityId, Vector3D jumpTarget, long userId)
        {
            MyCubeGrid grid = MyAPIGateway.Entities.GetEntityById(entityId) as MyCubeGrid;

            if (grid == null)
            {
                return false;
            }
     

            //return false;
            if (userId == 0)
            {

                Log.Info("grid name " + grid.DisplayName);
                Log.Info(FacUtils.GetOwner(grid) + " grid owner id, requested by 0, which is probably a hacker or some shit, these are the people online at the time");
                StringBuilder players = new StringBuilder();
                foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
                {
                    if (player.Id != null && player.Id.SteamId != null && player.DisplayName != null)
                    {
                        players.AppendLine(player.Id.SteamId + " " + player.DisplayName);
                    }
                    else
                    {
                        players.AppendLine("Something null here, identity id " + player.Identity.IdentityId);
                    }

                }
                Log.Info(players);
                if (AlliancePlugin.config.DisableJumpsWithId0)
                {

                    return false;
                }
            }
            else
            {
                Log.Info(FacUtils.GetOwner(grid) + " grid owner id, requested by " + userId + " grid name " + grid.DisplayName);
            }
            foreach (JumpZone zone in Zones)
            {


                float distance = Vector3.Distance(zone.GetPosition(), grid.PositionComp.GetPosition());

                if (distance <= zone.Radius && !zone.AllowExit)
                {
                    if (zone.GetExcludedExit() != null && zone.AllowExcludedExit)
                    {
                        bool canExit = false;
                        foreach (MyJumpDrive drive in grid.GetFatBlocks().OfType<MyJumpDrive>())
                        {
                            if (!zone.GetExcludedExit().Contains(drive.BlockDefinition.BlockPairName))
                            {
                                drive.Enabled = false;
                            }
                            else
                            {
                                canExit = true;
                            }

                        }
                        if (canExit)
                        {

                            // newPos = grid.WorldMatrix.Forward + 1000;
                            //worldMatrix = MatrixD.CreateWorld(newPos, grid.WorldMatrix.Forward, grid.WorldMatrix.Up);
                            return true;
                        }

                    }
                    NotificationMessage message = new NotificationMessage("You cannot jump out of this area.", 8000, "Red");
                    //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                    ModCommunication.SendMessageTo(message, MySession.Static.Players.TryGetSteamId(userId));
                    return false;
                }

                distance = Vector3.Distance(zone.GetPosition(), jumpTarget);

                if (distance <= zone.Radius && !zone.AllowEntry)
                {
                    if (zone.GetExcludedEntry() != null && zone.AllowExcludedEntry)
                    {
                        bool canExit = false;
                        foreach (MyJumpDrive drive in grid.GetFatBlocks().OfType<MyJumpDrive>())
                        {
                            if (!zone.GetExcludedExit().Contains(drive.BlockDefinition.BlockPairName))
                            {
                                drive.Enabled = false;
                            }
                            else
                            {
                                canExit = true;
                            }
                        }
                        if (canExit)
                        {
                            //newPos = grid.WorldMatrix.Forward + 1000;
                            // worldMatrix = MatrixD.CreateWorld(newPos, grid.WorldMatrix.Forward, grid.WorldMatrix.Up);
                            return true;
                        }
                    }
                    NotificationMessage message = new NotificationMessage("You cannot jump into this area.", 8000, "Red");
                    //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                    ModCommunication.SendMessageTo(message, MySession.Static.Players.TryGetSteamId(userId));
                    return false;
                }
            }

            //JumpThing thing = new JumpThing();
            //MyCockpit controller = null;
            //foreach (MyCockpit cockpit in grid.GetFatBlocks().OfType<MyCockpit>())
            //{
            //    if (cockpit.Pilot != null)
            //    {
            //        if (cockpit.Pilot.ControlSteamId.Equals(MySession.Static.Players.TryGetSteamId(userId)))
            //        {
            //            controller = cockpit;
            //        }
            //    }
            //}
            //AlliancePlugin.Log.Info("1");
            //if (controller != null)
            //{
            //    AlliancePlugin.Log.Info("2");
            //    MatrixD worldMatrix = MatrixD.CreateWorld(controller.WorldMatrix.Translation, controller.WorldMatrix.Forward, controller.WorldMatrix.Up);
            //    Vector3D distance2 = worldMatrix.Forward * 1000;
            //    worldMatrix.Translation += distance2;
            //    thing.matrix = worldMatrix;
            //    thing.gridId = grid.EntityId;
            //    AlliancePlugin.jumpies.Add(thing);
            //    return false;

            //}
            return true;
        }

    }
}
