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

        public string CityType = "Put a name here like manufacturing tier 8145";
        public int CityRadius = 50000;
        public long SafeZoneBlockId { get; set; }
        public string SafeZoneSubTypeId { get; set; }
        public DateTime TimeCanInit { get; set; }
        public bool HasInit = false;
        public long DiscordChannelId { get; set; }
        public int SiegeProgress = 0;
        public int SiegePointsToDropSafeZone = 30;
        public int SecondsBeforeCityOperational = 600;
        public string WorldName { get; set; }

        public Guid AllianceId { get; set; }

        public List<CraftedItem> CraftableItems = new List<CraftedItem>();
        public List<SpawnedItem> SpawnedItems = new List<SpawnedItem>();
        public int SecondsBetweenCrafting = 3600;
        public DateTime nextCraftRefresh = DateTime.Now;
        public long SpaceCreditsToCityOwners = 0;
        public int SecondsBetweenCreditPayout = 3600;

        public double ShipyardSpeedBuffPercent = 0.1;

        public Boolean EnableStationCrafting = false;
        public DateTime NextPayoutTime;

        public class RecipeItem
        {
            public string TypeId;
            public string SubtypeId;
            public long SpaceCreditsPerCraft;
            public int Amount;
        }

        public class SpawnedItem
        {
            public string TypeId;
            public string SubtypeId;
            public int AmountPerSpawn = 0;
            public DateTime NextSpawn;
            public int SecondsBetweenSpawns = 6000;
        }

        public class CraftedItem
        {
            public string TypeId;
            public string SubtypeId;
            public double ChanceToCraft = 0.5;
            public long SpaceCreditsPerCraft = 0;
            public int AmountPerCraft;
            public List<RecipeItem> RequiredItems = new List<RecipeItem>();
            public int SecondsBetweenCycles;
            public DateTime NextCraftCycle;
        }
    }
}
