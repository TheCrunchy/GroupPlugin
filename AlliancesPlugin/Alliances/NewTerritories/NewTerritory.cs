using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Alliances.NewTerritories
{
    public class NewTerritory
    {
        public Guid OwningAlliance { get; set; }
        public String TerritoryName { get; set; }
        public int TerritoryRadius { get; set; }

        public bool CountsForRefineryBuffs { get; set; }
        public bool CountsForAssemblerBuffs { get; set; }

        public long SpaceCreditsToCityOwners = 0;
        public int SecondsBetweenCreditPayout = 3600;

        public DateTime NextPayoutTime;
        public List<City> ActiveCities = new List<City>();
        public Boolean EnableStationCrafting = false;

    }
}
