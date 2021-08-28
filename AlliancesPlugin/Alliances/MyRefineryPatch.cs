using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
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

namespace AlliancesPlugin.Alliances
{
    [PatchShim]
    public static class MyRefineryPatch
    {
        internal static readonly MethodInfo update =
        typeof(MyRefinery).GetMethod("ChangeRequirementsToResults", BindingFlags.Instance | BindingFlags.NonPublic) ??
        throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo patch =
            typeof(MyRefineryPatch).GetMethod(nameof(ChangeRequirementsToResults), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        public static MethodInfo RemoveQueue;
        public static Boolean Enabled = true;
        public static void Patch(PatchContext ctx)
        {

            ctx.GetPattern(update).Prefixes.Add(patch);
        }
        public static Dictionary<int, RefineryUpgrade> upgrades = new Dictionary<int, RefineryUpgrade>();

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
                      //      AlliancePlugin.Log.Info(refin.BlockDefinition.Id.SubtypeName);
                            buff += upgrade.getRefineryBuff(refin.BlockDefinition.Id.SubtypeName);
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
