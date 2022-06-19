using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Alliances.Upgrades
{
    public class LoadedShipLimits
    {
        public static List<ShipClassUpgrade> LoadedShipUpgrades = new List<ShipClassUpgrade>();

        public static ShipClassUpgrade GetUpgrade(int UpgradeNum, string ClassName)
        {
            var item = LoadedShipUpgrades.FirstOrDefault(x => x.UpgradeId == UpgradeNum && x.ClassNameToUpgrade == ClassName);
            return item;
        }
    }
}
