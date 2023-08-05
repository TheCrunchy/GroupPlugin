using System;
using System.Collections.Generic;
using VRage.Game;

namespace AlliancesPlugin.Alliances.Upgrades
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
        private Dictionary<string, double> buffedTerritory = new Dictionary<string, double>();
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
        public double getRefineryBuffTerritory(string subtype)
        {
            if (buffedTerritory.TryGetValue("all", out double b))
            {
                return b;
            }
            if (buffedTerritory.TryGetValue(subtype, out double num))
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
                        if (!buffedTerritory.ContainsKey(refin.SubtypeId))
                        {
                            buffedTerritory.Add(refin.SubtypeId, buff.UpgradeGivesBuffInTerritory);
                        }
                    }
                }
            }
        }
        public class RefineryBuffList
        {
            public double UpgradeAddsYield = 0.025;
            public double UpgradeGivesBuffInTerritory = 0.03;
            public List<RefineryBuff> buffs = new List<RefineryBuff>();
        }
        public class RefineryBuff
        {
            public Boolean Enabled = false;
            public string SubtypeId = "LargeRefinery";
        }
    }
}
