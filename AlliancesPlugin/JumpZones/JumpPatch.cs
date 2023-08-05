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
using HarmonyLib;
using Sandbox.Game.GUI;

namespace AlliancesPlugin
{
    [PatchShim]
    public static class JumpPatch
    {
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
    typeof(MyJumpDrive).GetMethod("UpdateAfterSimulation100", BindingFlags.Instance | BindingFlags.Public) ??
    throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo DenyJumpPatch =
            typeof(JumpPatch).GetMethod(nameof(PatchRequestJump), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");
        public static void Patch(PatchContext ctx)
        {

        //    ctx.GetPattern(RequestJump).Prefixes.Add(DenyJumpPatch);

            //  ctx.GetPattern(RequestJump).Suffixes.Add(DenyJumpPatch2);
            Log.Info("Patching Successful jump drive stuff");
            ApplyLogging();
        }


        public static void PatchRequestJump(MyJumpDrive __instance)
        {
            MyCubeGrid grid = __instance.CubeGrid;
   

            if (grid == null)
            {
                return;
            }
           
            var target = grid.GridSystems.JumpSystem.GetJumpDriveTarget();
            if (grid.GridSystems.JumpSystem.IsJumping)
            {
                       grid.GridSystems.JumpSystem.AbortJump(MyGridJumpDriveSystem.MyJumpFailReason.Locked);
                //      Log.Info(target.ToString());
                foreach (MyCockpit cockpit in grid.GetFatBlocks().OfType<MyCockpit>())
                {
                    if (cockpit.Pilot != null)
                    {
                        NotificationMessage message = new NotificationMessage("You cannot jump into this area.", 8000, "Red");
                        //        //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                        ModCommunication.SendMessageTo(message, MySession.Static.Players.TryGetSteamId(cockpit.Pilot.GetPlayerIdentityId()));
                    }
                }

            }

         

            return;
        }

    }
}

//foreach (JumpZone zone in Zones)
//{


//    float distance = Vector3.Distance(zone.GetPosition(), grid.PositionComp.GetPosition());

//    if (distance <= zone.Radius && !zone.AllowExit)
//    {
//        if (zone.GetExcludedExit() != null && zone.AllowExcludedExit)
//        {
//            bool canExit = false;
//            foreach (MyJumpDrive drive in grid.GetFatBlocks().OfType<MyJumpDrive>())
//            {
//                if (!zone.GetExcludedExit().Contains(drive.BlockDefinition.BlockPairName))
//                {
//                    drive.Enabled = false;
//                }
//                else
//                {
//                    canExit = true;
//                }

//            }
//            if (canExit)
//            {

//                // newPos = grid.WorldMatrix.Forward + 1000;
//                //worldMatrix = MatrixD.CreateWorld(newPos, grid.WorldMatrix.Forward, grid.WorldMatrix.Up);
//                return true;
//            }

//        }
//        NotificationMessage message = new NotificationMessage("You cannot jump out of this area.", 8000, "Red");
//        //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
//        ModCommunication.SendMessageTo(message, MySession.Static.Players.TryGetSteamId(userId));
//        return false;
//    }

//    distance = Vector3.Distance(zone.GetPosition(), jumpTarget);

//    if (distance <= zone.Radius && !zone.AllowEntry)
//    {
//        if (zone.GetExcludedEntry() != null && zone.AllowExcludedEntry)
//        {
//            bool canExit = false;
//            foreach (MyJumpDrive drive in grid.GetFatBlocks().OfType<MyJumpDrive>())
//            {
//                if (!zone.GetExcludedExit().Contains(drive.BlockDefinition.BlockPairName))
//                {
//                    drive.Enabled = false;
//                }
//                else
//                {
//                    canExit = true;
//                }
//            }
//            if (canExit)
//            {
//                //newPos = grid.WorldMatrix.Forward + 1000;
//                // worldMatrix = MatrixD.CreateWorld(newPos, grid.WorldMatrix.Forward, grid.WorldMatrix.Up);
//                return true;
//            }
//        }
//        NotificationMessage message = new NotificationMessage("You cannot jump into this area.", 8000, "Red");
//        //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
//        ModCommunication.SendMessageTo(message, MySession.Static.Players.TryGetSteamId(userId));
//        return false;
//    }
//}
