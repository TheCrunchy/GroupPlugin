using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Alliances.Upgrades
{
    public class ShipClassLimits
    {
        public List<ShipLimit> Limits = new List<ShipLimit>();

        private Dictionary<String, int> _limits = new Dictionary<string, int>();

        public int GetLimitForClass(string ClassName)
        {
            if (_limits.TryGetValue(ClassName, out int value))
            {
                return value;
            }

            return 0;
        }

        public void PutLimitsInDictionary()
        {
            foreach (var item in Limits)
            {
                if (!_limits.ContainsKey(item.ClassName))
                {
                    var upgrade = LoadedShipLimits.GetUpgrade(item.UpgradeLevel, item.ClassName);
                    if (upgrade != null)
                    {
                        _limits.Add(item.ClassName, upgrade.NewClassLimit);
                    }
                   
                }

            }
    
        }

        public class ShipLimit
        {
            public string ClassName = "Example";
            public int UpgradeLevel = 0;
        }

    }
}
