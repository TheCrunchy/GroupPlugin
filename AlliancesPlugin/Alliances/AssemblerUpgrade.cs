using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace AlliancesPlugin.Alliances
{
    public class AssemblerUpgrade
    {
        public int UpgradeId = 1;
        public Boolean Enabled = false;
        public long MoneyRequired = 5000000;
        public int MetaPointsRequired = 500000;
        public List<ItemRequirement> items = new List<ItemRequirement>();
        public List<AssemblerBuffList> buffedRefineries = new List<AssemblerBuffList>();
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
                            AlliancePlugin.Log.Error("Duplicate ID for Assembler upgrade items " + item.SubTypeId + " in " + UpgradeId);
                        }
                    }
                }
            }

            return temp;
        }
        public double getAssemblerBuff(string subtype)
        {
            if (buffed.TryGetValue("all", out double b))
            {
                return b;
            }
            if (buffed.TryGetValue(subtype, out double num))
            {
                return num;
            }
            return 0;
        }
        public double getAssemblerBuffTerritory(string subtype)
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
            foreach (AssemblerBuffList buff in buffedRefineries)
            {
                foreach (AssemblerBuff refin in buff.buffs)
                {
                    if (refin.Enabled)
                    {
                        if (!buffed.ContainsKey(refin.SubtypeId))
                        {
                            buffed.Add(refin.SubtypeId, buff.UpgradeGivesSpeedBuuf);
                        }
                        else
                        {
                            AlliancePlugin.Log.Error("Duplicate subtypeIds in this upgrade " + refin.SubtypeId);
                        }
                        if (!buffedTerritory.ContainsKey(refin.SubtypeId))
                        {
                            buffedTerritory.Add(refin.SubtypeId, buff.UpgradeGivesSpeedBuuf);
                        }
                    }
                }
            }
        }
        public class AssemblerBuffList
        {
            public double UpgradeGivesSpeedBuuf = 0.025;
            public double UpgradeGivesBuffInTerritory = 0.03;
            public List<AssemblerBuff> buffs = new List<AssemblerBuff>();
        }
        public class AssemblerBuff
        {
            public Boolean Enabled = false;
            public string SubtypeId = "LargeAssembler";
        }
    }
}
