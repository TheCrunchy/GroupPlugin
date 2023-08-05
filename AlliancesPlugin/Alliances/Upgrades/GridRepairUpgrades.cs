using System;
using System.Collections.Generic;
using VRage.Game;

namespace AlliancesPlugin.Alliances.Upgrades
{
    public class GridRepairUpgrades
    {
        public int UpgradeId = 0;
        public Boolean Enabled = false;
        public long MoneyRequired = 5000000;
        public int MetaPointsRequired = 500000;
        public List<ItemRequirement> items = new List<ItemRequirement>();
        //public long AddsToUpkeep = 50;

        public int SecondsPerCycle = 20;
        public int ProjectedBuildPerCycle = 10;
        public int RepairBlocksPerCycle = 10;
        public Dictionary<MyDefinitionId, int> getItemsRequired()
        {
            Dictionary<MyDefinitionId, int> temp = new Dictionary<MyDefinitionId, int>();
            foreach (ItemRequirement item in this.items)
            {
                if (item.Enabled)
                {
                    if (MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId, out MyDefinitionId id))
                    {
                        if (!temp.ContainsKey(id))
                        {
                            temp.Add(id, item.RequiredAmount);
                        }
                        else
                        {
                            AlliancePlugin.Log.Error("Duplicate ID for grid repair upgrade items " + item.SubTypeId + " in " + UpgradeId);
                        }
                    }
                }
            }

            return temp;
        }
        public long PriceIfNotDefined = 50000;
        public long getCost(String id)
        {
            if (ComponentCosts.ContainsKey(id))
            {
                return ComponentCosts[id];
            }
            else
            {
                return PriceIfNotDefined;
            }
        }
        private Dictionary<String, long> ComponentCosts = new Dictionary<string, long>();
        public List<ComponentCostForRepair> repairCost = new List<ComponentCostForRepair>();
        public void AddComponentCostToDictionary()
        {
            foreach (ComponentCostForRepair comp in repairCost)
            {
                if (!ComponentCosts.ContainsKey(comp.SubTypeId))
                {
                    ComponentCosts.Add(comp.SubTypeId, comp.Cost);
                }
            }
        }
        public class ComponentCostForRepair
        {
            public string SubTypeId;
            public long Cost = 1000;
        }

        public List<String> BannedComponents = new List<String>();
    }
}
