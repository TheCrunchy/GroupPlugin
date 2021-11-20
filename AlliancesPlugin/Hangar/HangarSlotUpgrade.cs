using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace AlliancesPlugin.Alliances
{
    public class HangarUpgrade
    {
        public int UpgradeId = 0;
        public Boolean Enabled = false;
        public double NewSlots = 0;
        public long MoneyRequired = 5000000;
        public int MetaPointsRequired = 500000;
        public List<ItemRequirement> items = new List<ItemRequirement>();
        //    public long AddsToUpkeep = 50;
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
                            AlliancePlugin.Log.Error("Duplicate ID for Hangar upgrade items " + item.SubTypeId + " in " + UpgradeId);
                        }
                    }
                }
            }

            return temp;
        }

    }
}