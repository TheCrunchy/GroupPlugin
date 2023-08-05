using System.Collections.Generic;
using System.Linq;

namespace AlliancesPlugin.Alliances.Upgrades.ShipClasses
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
