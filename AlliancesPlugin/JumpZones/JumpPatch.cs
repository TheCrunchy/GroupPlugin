using NLog;
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
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();




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
        }

        public static bool PatchRequestJump(long entityId, Vector3D jumpTarget, long userId)
        {
            foreach (JumpZone zone in Zones)
            {
               
                MyCubeGrid grid = MyAPIGateway.Entities.GetEntityById(entityId) as MyCubeGrid;
                float distance = Vector3.Distance(zone.GetPosition(), grid.PositionComp.GetPosition());
                Log.Info(distance);
                if (distance <= zone.Radius && !zone.AllowExit)
                {
                    if (zone.GetExcludedExit() != null && zone.AllowExcludedExit)
                    {
                            foreach (MyJumpDrive drive in grid.GetFatBlocks().OfType<MyJumpDrive>())
                            {
                                if (!zone.GetExcludedExit().Contains(drive.BlockDefinition.BlockPairName))
                                {
                                    drive.Enabled = false;
                                }
                            }
                            return true;
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
                        foreach (MyJumpDrive drive in grid.GetFatBlocks().OfType<MyJumpDrive>())
                        {
                            if (!zone.GetExcludedExit().Contains(drive.BlockDefinition.BlockPairName))
                            {
                                drive.Enabled = false;
                            }
                        }
                        return true;
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
