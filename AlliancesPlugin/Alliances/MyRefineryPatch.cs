using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;

namespace AlliancesPlugin.Alliances
{
    public class MyRefineryPatch
    {
        public static Boolean Enabled = false;

        private void ChangeRequirementsToResults(
     MyBlueprintDefinitionBase queueItem,
     MyFixedPoint blueprintAmount, MyRefinery __instance)
        {
            MyRefinery refin = __instance;
            if (!Enabled)
            {
                return;
            }
            if (refin.BlockDefinition as MyRefineryDefinition == null)
            {
                return;
            }
            {
                if (!Sync.IsServer || MySession.Static == null || (queueItem == null || queueItem.Prerequisites == null) || (refin.OutputInventory == null || refin.InputInventory == null || (queueItem.Results == null)))
                    return;
                if (!MySession.Static.CreativeMode)
                    blueprintAmount = MyFixedPoint.Min(refin.OutputInventory.ComputeAmountThatFits(queueItem), blueprintAmount);
                if (blueprintAmount == (MyFixedPoint)0)
                    return;
                foreach (MyBlueprintDefinitionBase.Item result in queueItem.Results)
                {
                    if ((MyObjectBuilderSerializer.CreateNewObject((SerializableDefinitionId)result.Id) is MyObjectBuilder_PhysicalObject newObject))
                    {
                        MyRefineryDefinition def = refin.BlockDefinition as MyRefineryDefinition;
                        float num = (float)result.Amount * def.MaterialEfficiency * refin.UpgradeValues["Effectiveness"];
                        refin.OutputInventory.AddItems((MyFixedPoint)((float)blueprintAmount * num * 0.05f), (MyObjectBuilder_Base)newObject);
                    }
                }
                return;
            }
        }
    }
}
