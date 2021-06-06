using NLog;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace AlliancesPlugin.Special_Designation
{

    [PatchShim]
    public static class DrillPatch
    {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();



        internal static readonly MethodInfo update =
            typeof(MyDrillBase).GetMethod("TryHarvestOreMaterial", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo updatePatch =
            typeof(DrillPatch).GetMethod(nameof(TestPatchMethod), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");




        public static void Patch(PatchContext ctx)
        {

          //  ctx.GetPattern(update).Suffixes.Add(updatePatch);

            Log.Info("Patching Successful CrunchDrill!");

        }
        public static Type drill = null;

        public static void TestPatchMethod(MyDrillBase __instance, MyVoxelMaterialDefinition material,
      Vector3 hitPosition,
      int removedAmount,
      bool onlyCheck)
        {
            if (__instance.OutputInventory != null && __instance.OutputInventory.Owner != null)
            {
                if (__instance.OutputInventory.Owner.GetBaseEntity() is MyShipDrill shipDrill)
                {

                }
                else
                {
                    return;
                }
            } 
            if (drill == null) {
                drill = __instance.GetType();
            }

            if (string.IsNullOrEmpty(material.MinedOre))
                return;
            if (!onlyCheck)
            {
                MyObjectBuilder_Ore newObject = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>(material.MinedOre);
                newObject.MaterialTypeName = new MyStringHash?(material.Id.SubtypeId);
                float num = (float)((double)removedAmount / (double)byte.MaxValue * 1.0) * __instance.VoxelHarvestRatio * material.MinedOreRatio;
                if (!MySession.Static.AmountMined.ContainsKey(material.MinedOre))
                    MySession.Static.AmountMined[material.MinedOre] = (MyFixedPoint)0;
                MySession.Static.AmountMined[material.MinedOre] += (MyFixedPoint)num;
                MyPhysicalItemDefinition physicalItemDefinition = MyDefinitionManager.Static.GetPhysicalItemDefinition((MyObjectBuilder_Base)newObject);
                MyFixedPoint amountItems1 = (MyFixedPoint)(num / physicalItemDefinition.Volume);
                MyFixedPoint maxAmountPerDrop = (MyFixedPoint)(float)(0.150000005960464 / (double)physicalItemDefinition.Volume);

               
                
             
                MyFixedPoint collectionRatio = (MyFixedPoint) drill.GetField("m_inventoryCollectionRatio", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
               MyFixedPoint b = amountItems1 * ((MyFixedPoint)1 - collectionRatio);
                MyFixedPoint amountItems2 = MyFixedPoint.Min(maxAmountPerDrop * 10 - (MyFixedPoint)0.001, b);
                MyFixedPoint totalAmount = amountItems1 * collectionRatio - amountItems2;
                Log.Info(totalAmount);

            }
        }
    }
}
