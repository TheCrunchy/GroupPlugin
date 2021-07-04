using NLog;
using NLog.Config;
using NLog.Targets;
using Sandbox.Game.Entities;
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

            //  ctx.GetPattern(update).Prefixes.Add(StartJumpPatch);
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
            Log.Info(FacUtils.GetOwner(grid) + " grid owner id, requested by " + userId);
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
                            return true;
                        }

                    }
                    NotificationMessage message = new NotificationMessage("You cannot jump out of this area.", 8000, "Red");
                    //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                    ModCommunication.SendMessageTo(message, MySession.Static.Players.TryGetSteamId(userId));
                    return false;
                }

                distance = Vector3.Distance(zone.GetPosition(), jumpTarget);
                Log.Info(distance);
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
                            return true;
                        }
                    }
                    NotificationMessage message = new NotificationMessage("You cannot jump into this area.", 8000, "Red");
                    //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible
                    ModCommunication.SendMessageTo(message, MySession.Static.Players.TryGetSteamId(userId));
                    return false;
                }
            }
            return true;
        }

    }
}
