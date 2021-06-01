using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;
using VRageMath;

namespace AlliancesPlugin
{
    [PatchShim]
    public static class JumpPatch
    {
        public static List<JumpZone> Zones = new List<JumpZone>();
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        internal static readonly MethodInfo update =
            typeof(MyJumpDrive).GetMethod("RequestJump", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo StartJumpPatch =
            typeof(JumpPatch).GetMethod(nameof(PatchRequestJump), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");



        internal static readonly MethodInfo RequestJump =
    typeof(MyGridJumpDriveSystem).GetMethod("RequestJump", BindingFlags.Instance | BindingFlags.Public,null, new Type[] { typeof(String), typeof(Vector3D), typeof(long)}, null) ??
    throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo DenyJumpPatch =
            typeof(JumpPatch).GetMethod(nameof(PatchRequestJump2), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");
        public static void Patch(PatchContext ctx)
        {

            ctx.GetPattern(update).Prefixes.Add(StartJumpPatch);
            ctx.GetPattern(RequestJump).Prefixes.Add(DenyJumpPatch);
            Log.Info("Patching Successful jump drive stuff");
        }

        public static bool PatchRequestJump(MyJumpDrive __instance, bool usePlayer = true)
        {
            foreach (JumpZone zone in Zones)
            {
                if (zone.AllowExit)
                    continue;

                float distance = Vector3.Distance(zone.GetPosition(), __instance.PositionComp.GetPosition());
                if (distance < zone.Radius)
                {
                    if (zone.GetExcludedExit() != null && zone.AllowExcludedExit)
                    {
                        if (zone.GetExcludedExit().Contains(__instance.BlockDefinition.BlockPairName))
                        {
                            foreach (MyJumpDrive drive in __instance.CubeGrid.GetFatBlocks().OfType<MyJumpDrive>())
                            {
                                if (!zone.GetExcludedExit().Contains(drive.BlockDefinition.BlockPairName))
                                {
                                    drive.Enabled = false;
                                }
                            }
                            return true;
                        }
                    }
                    return false;
                }
            }
            return true;
        }
        public static bool PatchRequestJump2(string destinationName, Vector3D destination, long userId)
        {
            foreach (JumpZone zone in Zones)
            {
                if (zone.AllowEntry)
                    continue;

                float distance = Vector3.Distance(zone.GetPosition(), destination);
                if (distance < zone.Radius)
                {

                    return false;
                }
            }
            //Do logic for if its inside a denied zone, loop over each one and check distance between grid and it

            return true;
        }
    }
}
