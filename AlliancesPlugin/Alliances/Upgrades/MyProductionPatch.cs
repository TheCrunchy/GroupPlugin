using AlliancesPlugin.Alliances.NewTerritories;
using AlliancesPlugin.KOTH;
using HarmonyLib;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
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
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;

namespace AlliancesPlugin.Alliances
{
    [PatchShim]
    public static class MyProductionPatch
    {
        internal static readonly MethodInfo update =
        typeof(MyRefinery).GetMethod("ChangeRequirementsToResults", BindingFlags.Instance | BindingFlags.NonPublic) ??
        throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo patch =
            typeof(MyProductionPatch).GetMethod(nameof(ChangeRequirementsToResults), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        public static MethodInfo RemoveQueue;
        public static Boolean Enabled = true;
        public static void Patch(PatchContext ctx)
        {

            ctx.GetPattern(update).Prefixes.Add(patch);
        }
        public static Dictionary<int, RefineryUpgrade> upgrades = new Dictionary<int, RefineryUpgrade>();
        public static Dictionary<int, AssemblerUpgrade> assemblerupgrades = new Dictionary<int, AssemblerUpgrade>();
        public static Boolean YEET = false;

        [HarmonyPatch(typeof(MyAssembler))]
        [HarmonyPatch("CalculateBlueprintProductionTime")]
        public class AssemblerPatch
        {
            static void Postfix(MyBlueprintDefinitionBase currentBlueprint, ref float __result, MyAssembler __instance)
            {

                if (__instance.GetOwnerFactionTag().Length > 0)
                {
                    Alliance alliance = AlliancePlugin.GetAllianceNoLoading(MySession.Static.Factions.TryGetFactionByTag(__instance.GetOwnerFactionTag()));
                    if (alliance == null)
                    {
                        return;
                    }
                    if (alliance.AssemblerUpgradeLevel == 0)
                    {
                        //     AlliancePlugin.Log.Info("no refinery upgrade");
                        return;
                    }

                   // MyAPIGateway.Multiplayer.RegisterMessageHandler(NET_ID, MessageHandler);

                    float buff = 1f;
                    //    AlliancePlugin.Log.Info("Buffed by " + buff.ToString());
                    if (assemblerupgrades.TryGetValue(alliance.AssemblerUpgradeLevel, out AssemblerUpgrade upgrade))
                    {
                            if (TimeChecks.TryGetValue(__instance.EntityId, out DateTime time))
                            {
                                if (DateTime.Now >= time)
                                {
                                    TimeChecks[__instance.EntityId] = DateTime.Now.AddMinutes(1);
                                    if (InsideHere.TryGetValue(__instance.EntityId, out Guid terId))
                                    {
                                        if (AlliancePlugin.Territories.TryGetValue(terId, out Territory ter))
                                        {
                                            float distance = Vector3.Distance(__instance.CubeGrid.PositionComp.GetPosition(), new Vector3(ter.x, ter.y, ter.z));
                                            if (distance <= ter.Radius)
                                            {
                                                IsInsideTerritory.Remove(__instance.EntityId);
                                                IsInsideTerritory.Add(__instance.EntityId, true);
                                            }
                                            else
                                            {
                                                InsideHere.Remove(__instance.EntityId);
                                                IsInsideTerritory.Remove(__instance.EntityId);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (Territory ter in AlliancePlugin.Territories.Values)
                                        {
                                            if (ter.Alliance != Guid.Empty && ter.Alliance == alliance.AllianceId)
                                            {
                                                float distance = Vector3.Distance(__instance.CubeGrid.PositionComp.GetPosition(), new Vector3(ter.x, ter.y, ter.z));
                                                if (distance <= ter.Radius)
                                                {
                                                    IsInsideTerritory.Remove(__instance.EntityId);
                                                    IsInsideTerritory.Add(__instance.EntityId, true);
                                                    InsideHere.Remove(__instance.EntityId);

                                                    InsideHere.Add(__instance.EntityId, ter.Id);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                TimeChecks.Add(__instance.EntityId, DateTime.Now.AddMinutes(0.01));
                            }
                            //      AlliancePlugin.Log.Info(refin.BlockDefinition.Id.SubtypeName);
                            if (IsInsideTerritory.TryGetValue(__instance.EntityId, out bool isInside))
                            {
                                if (isInside)
                                {

                                    buff -= (float)upgrade.getAssemblerBuffTerritory(__instance.BlockDefinition.Id.SubtypeName);
                                }
                                else
                                {
                                    buff -= (float)upgrade.getAssemblerBuff(__instance.BlockDefinition.Id.SubtypeName);
                                }
                            }
                            else
                            {
                                buff -= (float)upgrade.getAssemblerBuff(__instance.BlockDefinition.Id.SubtypeName);
                            }
                        //      AlliancePlugin.Log.Info(refin.BlockDefinition.Id.SubtypeName);

                        __result *= buff;
                    }
                }
            }
        } 
        public static Dictionary<long, bool> IsInsideTerritory = new Dictionary<long, bool>();
        public static Dictionary<long, DateTime> TimeChecks = new Dictionary<long, DateTime>();
        public static Dictionary<long, Guid> InsideHere = new Dictionary<long, Guid>();

        public static Boolean ChangeRequirementsToResults(
     MyBlueprintDefinitionBase queueItem,
     MyFixedPoint blueprintAmount, MyRefinery __instance)
        {
          
            
            MyRefinery refin = __instance;
            if (!Enabled)
            {
                return true;
            }
            if (refin.BlockDefinition as MyRefineryDefinition == null)
            {
                return false;
            }
            {
                if (!Sync.IsServer || MySession.Static == null || (queueItem == null || queueItem.Prerequisites == null) || (refin.OutputInventory == null || refin.InputInventory == null || (queueItem.Results == null)))
                    return false;
                if (!MySession.Static.CreativeMode)
                    blueprintAmount = MyFixedPoint.Min(refin.OutputInventory.ComputeAmountThatFits(queueItem), blueprintAmount);
                if (blueprintAmount == (MyFixedPoint)0)
                    return false;
                if (refin.GetOwnerFactionTag().Length > 0)
                {
                    Alliance alliance = AlliancePlugin.GetAllianceNoLoading(MySession.Static.Factions.TryGetFactionByTag(refin.GetOwnerFactionTag()));
                    if (alliance == null)
                    {
                        //          AlliancePlugin.Log.Info("no alliance");
                        return true;
                    }
                    if (alliance.RefineryUpgradeLevel == 0)
                    {
                        //     AlliancePlugin.Log.Info("no refinery upgrade");
                        return true;
                    }
                    else
                    {

                        double buff = 1;
                        //    AlliancePlugin.Log.Info("Buffed by " + buff.ToString());
                  
                        if (upgrades.TryGetValue(alliance.RefineryUpgradeLevel, out RefineryUpgrade upgrade))
                        {
                            if (TimeChecks.TryGetValue(refin.EntityId, out DateTime time))
                            {
                                if (DateTime.Now >= time)
                                {
                                    TimeChecks[refin.EntityId] = DateTime.Now.AddMinutes(1);
                                    if (InsideHere.TryGetValue(refin.EntityId, out Guid terId))
                                    {
                                        if (AlliancePlugin.Territories.TryGetValue(terId, out Territory ter))
                                        {
                                            float distance = Vector3.Distance(refin.CubeGrid.PositionComp.GetPosition(), new Vector3(ter.x, ter.y, ter.z));
                                            if (distance <= ter.Radius)
                                            {
                                                IsInsideTerritory.Remove(refin.EntityId);
                                                IsInsideTerritory.Add(refin.EntityId, true);
                                            }
                                            else
                                            {
                                                InsideHere.Remove(refin.EntityId);
                                                IsInsideTerritory.Remove(refin.EntityId);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (Territory ter in AlliancePlugin.Territories.Values)
                                        {
                                            if (ter.Alliance != Guid.Empty && ter.Alliance == alliance.AllianceId)
                                            {
                                                float distance = Vector3.Distance(refin.CubeGrid.PositionComp.GetPosition(), new Vector3(ter.x, ter.y, ter.z));
                                                if (distance <= ter.Radius)
                                                {
                                                    IsInsideTerritory.Remove(refin.EntityId);
                                                    IsInsideTerritory.Add(refin.EntityId, true);
                                                    InsideHere.Remove(refin.EntityId);

                                                    InsideHere.Add(refin.EntityId, ter.Id);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                TimeChecks.Add(refin.EntityId, DateTime.Now.AddMinutes(0.01));
                            }
                            //      AlliancePlugin.Log.Info(refin.BlockDefinition.Id.SubtypeName);
                            if (IsInsideTerritory.TryGetValue(refin.EntityId, out bool isInside))
                            {
                                if (isInside)
                                {
                                 //   AlliancePlugin.Log.Info("inside territory");
                                    buff += upgrade.getRefineryBuffTerritory(refin.BlockDefinition.Id.SubtypeName);
                                }
                                else
                                {
                                 //   AlliancePlugin.Log.Info("not inside territory");
                                    buff += upgrade.getRefineryBuff(refin.BlockDefinition.Id.SubtypeName);
                                }
                            }
                            else
                            {
                             //   AlliancePlugin.Log.Info("not inside territory");
                                buff += upgrade.getRefineryBuff(refin.BlockDefinition.Id.SubtypeName);
                            }

                            //    AlliancePlugin.Log.Info(buff);
                            foreach (MyBlueprintDefinitionBase.Item prerequisite in queueItem.Prerequisites)
                            {
                                if ((MyObjectBuilderSerializer.CreateNewObject((SerializableDefinitionId)prerequisite.Id) is MyObjectBuilder_PhysicalObject newObject))
                                {
                                    refin.InputInventory.RemoveItemsOfType((MyFixedPoint)((float)blueprintAmount * (float)prerequisite.Amount), newObject, false, false);
                                    MyFixedPoint itemAmount = refin.InputInventory.GetItemAmount(prerequisite.Id, MyItemFlags.None, false);
                                    if (itemAmount < (MyFixedPoint)0.01f)
                                        refin.InputInventory.RemoveItemsOfType(itemAmount, prerequisite.Id, MyItemFlags.None, false);
                                }
                            }
                            foreach (MyBlueprintDefinitionBase.Item result in queueItem.Results)
                            {
                                if ((MyObjectBuilderSerializer.CreateNewObject((SerializableDefinitionId)result.Id) is MyObjectBuilder_PhysicalObject newObject))
                                {
                                    MyRefineryDefinition def = refin.BlockDefinition as MyRefineryDefinition;
                                    float num = (float)result.Amount * def.MaterialEfficiency * refin.UpgradeValues["Effectiveness"];
                                    refin.OutputInventory.AddItems((MyFixedPoint)((float)blueprintAmount * num * buff), (MyObjectBuilder_Base)newObject);
                                }
                            }

                            //  ref.RemoveFirstQueueItemAnnounce(blueprintAmount, 0.0f);
                            if (RemoveQueue == null)
                            {
                                Type change = refin.GetType().Assembly.GetType("Sandbox.Game.Entities.Cube.MyProductionBlock");
                                RemoveQueue = change.GetMethod("RemoveFirstQueueItemAnnounce", BindingFlags.NonPublic | BindingFlags.Instance);


                            }
                            object[] MethodInput = new object[] { blueprintAmount, 0.0f };
                            RemoveQueue.Invoke(refin, MethodInput);

                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
