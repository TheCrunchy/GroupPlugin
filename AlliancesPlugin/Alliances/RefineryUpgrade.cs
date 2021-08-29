using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace AlliancesPlugin.Alliances
{
    public class RefineryUpgrade
    {
        public int UpgradeId = 1;
        public Boolean Enabled = false;
        public long MoneyRequired = 5000000;
        public int MetaPointsRequired = 500000;
        public List<ItemRequirement> items = new List<ItemRequirement>();
        public List<RefineryBuffList> buffedRefineries = new List<RefineryBuffList>();
        private Dictionary<string, double> buffed = new Dictionary<string, double>();
        public Dictionary<MyDefinitionId, int> getItemsRequired()
        {
            Dictionary<MyDefinitionId, int> temp = new Dictionary<MyDefinitionId, int>();
            foreach (RefineryUpgrade.ItemRequirement item in this.items)
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
                            AlliancePlugin.Log.Error("Duplicate ID for refinery upgrade items " + item.SubTypeId + " in " + UpgradeId);
                        }
                    }
                }
            }

            return temp;
        }
        public double getRefineryBuff(string subtype)
        {
            if (buffed.TryGetValue("all", out double b)){
                return b;
            }
            if (buffed.TryGetValue(subtype, out double num))
            {
                return num;
            }
            return 0;
        }

        public void PutBuffedInDictionary()
        {
            foreach (RefineryBuffList buff in buffedRefineries)
            {
                foreach (RefineryBuff refin in buff.buffs)
                {
                    if (refin.Enabled)
                    {
                        if (!buffed.ContainsKey(refin.SubtypeId))
                        {
                            buffed.Add(refin.SubtypeId, buff.UpgradeAddsYield);
                        }
                        else
                        {
                            AlliancePlugin.Log.Error("Duplicate subtypeIds in this upgrade " + refin.SubtypeId);
                        }
                    }
                }
            }
        }
        public class RefineryBuffList
        {
            public double UpgradeAddsYield = 0.025;
            public List<RefineryBuff> buffs = new List<RefineryBuff>();
        }
        public class RefineryBuff
        {
            public Boolean Enabled = false;
            public string SubtypeId = "LargeRefinery";
        }
        public class ItemRequirement
        {
            public Boolean Enabled = false;
            public int RequiredAmount = 50;
            public string TypeId = "Ingot";
            public string SubTypeId = "Uranium";

        }
    }
}
