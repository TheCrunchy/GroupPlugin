using AlliancesPlugin.Alliances.NewTerritories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.KOTH
{
    public class Territory
    {
        public Guid Id = System.Guid.NewGuid();
        public string Name = "Unnamed";
        public int Radius = 50000;
        public bool enabled = true;
        public Guid Alliance = Guid.Empty;

        public string EntryMessage = "You are in {name} Territory";
        public string ControlledMessage = "Controlled by {alliance}";
        public string ExitMessage = "You have left {name} Territory";
        public double x;
        public double y;
        public double z;

        public bool CountsForRefineryBuffs { get; set; }
        public bool CountsForAssemblerBuffs { get; set; }

        public List<City> ActiveCities = new List<City>();
        public int MaximumCities = 5;

        public Boolean TaxesForStationsInTerritory = false;

        public float TaxPercent = 0.02f;
        public List<TaxPercentAlliance> AllianceRates = new List<TaxPercentAlliance>();
        public float GetTaxRate(Guid allianceId)
        {
            foreach (TaxPercentAlliance percent in AllianceRates) {
                if (percent.AllianceId == allianceId)
                {
                    return percent.TaxPercent;
                }
            }

            return TaxPercent;
        }

        public class TaxPercentAlliance
        {
            public Guid AllianceId;
            public float TaxPercent = 0.02f;
        }
    }
}
