using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Alliances.NewTerritories
{
    public class City
    {
        public Guid CityId = Guid.NewGuid();
        public long GridId { get; set; }
        public string SafeZoneSubTypeId { get; set; }
        public DateTime TimeCanInit { get; set; }

        public int SiegeProgress = 0;

        public string WorldName { get; set; }

        public Guid AllianceId { get; set; }

        public List<CraftedItem> CraftableItems = new List<CraftedItem>();

        public int SecondsBetweenCrafting = 3600;
        public DateTime nextCraftRefresh = DateTime.Now;
        public long SpaceCreditsToCityOwners = 0;
        public int SecondsBetweenCreditPayout = 3600;

        public int ShipyardSlotsProvided = 0;

        public Boolean EnableStationCrafting = false;
        public DateTime NextPayoutTime;

        public class RecipeItem
        {
            public string typeid;
            public string subtypeid;
            public int amount;
        }

        public class CraftedItem
        {
            public string typeid;
            public string subtypeid;
            public double chanceToCraft = 0.5;
            public int amountPerCraft;
            public List<RecipeItem> RequriedItems = new List<RecipeItem>();
            public int secondsBetweenCycles;
            public DateTime nextCraftCycle;
        }
    }
}
