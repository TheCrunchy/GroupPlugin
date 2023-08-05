using System;
using System.Collections.Generic;
using VRage.Game;

namespace AlliancesPlugin.Alliances.Upgrades.ShipClasses
{
    public class ShipClassUpgrade
    {
        public Boolean Enabled = false;
        public string ClassNameToUpgrade = "Example";
        public int NewClassLimit = 2;
        public int UpgradeId = 1;
        public List<ItemRequirement> items = new List<ItemRequirement>();
        public long MoneyRequired = 5000000;
        public long AddsToUpkeep = 50;
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
                            AlliancePlugin.Log.Error("Duplicate ID for ShipClass upgrade items " + item.SubTypeId + " in " + UpgradeId);
                        }
                    }
                }
            }

            return temp;
        }
    }
}
