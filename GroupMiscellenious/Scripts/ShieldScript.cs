using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup;
using NLog.LayoutRenderers;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Managers.PatchManager;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;


namespace GroupMiscellenious.Scripts
{

    public class GridClass
    {
        public MyCubeGrid MainGrid { get; set; }
        public MyBatteryBlock BatteryBlock { get; set; }
    }

    [PatchShim]
    public class ShieldScript
    {
        public static int ticks = 0;
        public static Dictionary<long, GridClass> GridsWithShields = new Dictionary<long, GridClass>();

        public static void UpdateExample()
        {
            ticks++;
            if (ticks % 100 == 0)
            {
                foreach (var grid in GridsWithShields)
                {
                    if (grid.Value.BatteryBlock == null)
                    {
                        var battery = grid.Value.MainGrid.GetFatBlocks().OfType<MyBatteryBlock>().FirstOrDefault(x =>
                            x.BlockDefinition.Id.SubtypeName.Contains("LargeBlockBatteryBlockTEST"));
                        if (battery == null)
                        {
                            continue;
                        }

                        grid.Value.BatteryBlock = battery;
                    }

                    var charge = grid.Value.BatteryBlock.CurrentStoredPower / grid.Value.BatteryBlock.MaxStoredPower *
                                 100;
                    var current = grid.Value.MainGrid.GridGeneralDamageModifier.Value;
                    if (charge <= 10)
                    {
                        return;
                    }

                    if (charge <= 25)
                    {
                        if (current != 0.9f)
                        {
                            grid.Value.MainGrid.GridGeneralDamageModifier.ValidateAndSet(0.9f);
                            var pilot = grid.Value.MainGrid.GetFatBlocks().OfType<MyCockpit>().Where(x => x.Pilot != null);
                            foreach (var character in pilot)
                            {
                                Core.SendChatMessage("Shields", "Shields set to 10% resistance", character.Pilot.ControlSteamId);
                            }
                        }

                        continue;
                    }

                    if (charge <= 50)
                    {
                        if (current != 0.85f)
                        {
                            grid.Value.MainGrid.GridGeneralDamageModifier.ValidateAndSet(0.85f);
                            var pilot = grid.Value.MainGrid.GetFatBlocks().OfType<MyCockpit>().Where(x => x.Pilot != null);
                            foreach (var character in pilot)
                            {
                                Core.SendChatMessage("Shields", "Shields set to 15% resistance", character.Pilot.ControlSteamId);
                            }
                        }
                        continue;
                    }

                    if (charge <= 75)
                    {
                        if (current != 0.75f)
                        {
                            grid.Value.MainGrid.GridGeneralDamageModifier.ValidateAndSet(0.75f);
                            var pilot = grid.Value.MainGrid.GetFatBlocks().OfType<MyCockpit>().Where(x => x.Pilot != null);
                            foreach (var character in pilot)
                            {
                                Core.SendChatMessage("Shields", "Shields set to 15% resistance", character.Pilot.ControlSteamId);
                            }
                        }
                        continue;
                    }

                    if (charge <= 100)
                    {
                        if (current != 0.1f)
                        {
                            grid.Value.MainGrid.GridGeneralDamageModifier.ValidateAndSet(0.1f);
                            var pilot = grid.Value.MainGrid.GetFatBlocks().OfType<MyCockpit>().Where(x => x.Pilot != null);
                            foreach (var character in pilot)
                            {
                                Core.SendChatMessage("Shields", "Shields set to 90% resistance", character.Pilot.ControlSteamId);
                            }
                        }
                        continue;
                    }
                }
            }
        }


        internal static readonly MethodInfo DamageRequest =
            typeof(MySlimBlock).GetMethod("DoDamage", BindingFlags.Instance | BindingFlags.Public, null,
                new Type[]
                {
                    typeof(float), typeof(MyStringHash), typeof(bool), typeof(MyHitInfo?), typeof(long), typeof(long),
                    typeof(bool)
                }, null) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo patchSlimDamage =
            typeof(ShieldScript).GetMethod(nameof(OnDamageRequest), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        public static void OnDamageRequest(MySlimBlock __instance, float damage,
        MyStringHash damageType,
        bool sync,
        MyHitInfo? hitInfo,
        long attackerId, long realHitEntityId = 0, bool shouldDetonateAmmo = true)
        {
            if (__instance.FatBlock != null)
            {
                if (GridsWithShields.TryGetValue(__instance.CubeGrid.EntityId, out var gridsClass))
                {
                    if (gridsClass.BatteryBlock != null)
                    {
                        gridsClass.BatteryBlock.CurrentStoredPower -= damage / 100;
                        Core.Log.Info($"{damage}");
                    }
                }
            }
            return;
        }

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(DamageRequest).Suffixes.Add(patchSlimDamage);
            Core.UpdateCycle += UpdateExample;
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;

            // Iterate through all existing grids when the mod initializes
            var grids = new List<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(null, (entity) =>
            {
                if (entity is IMyCubeGrid grid)
                {
                    OnEntityAdd(grid);
                }
                return false;
            });
        }
        private static void OnEntityAdd(IMyEntity entity)
        {
            if (entity is IMyCubeGrid grid)
            {
                grid.OnBlockAdded += OnBlockAdded;
              
                var GridClass = new GridClass()
                {
                    MainGrid = grid as MyCubeGrid
                };
                GridsWithShields.Add(grid.EntityId, GridClass);
                GridClass.MainGrid.GridGeneralDamageModifier.ValidateAndSet(1f);
            }
        }

        private static void OnBlockAdded(IMySlimBlock block)
        {
            if (block.BlockDefinition != null && block.BlockDefinition.Id.SubtypeName.Contains("LargeBlockBatteryBlockTEST"))
            {
                var grid = block.CubeGrid as MyCubeGrid;
                var GridClass = new GridClass()
                {
                    MainGrid = grid
                };
                GridsWithShields[grid.EntityId] = GridClass;
            }
        }
    }
}
