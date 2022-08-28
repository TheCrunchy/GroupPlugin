using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Alliances.Radar
{
    public class RadarModel
    {
        public bool Enabled = false;
        public string BeaconBlockPairName = "AllianceRadar";
        public float AdditionalDetectionChance = 0;
        public bool OnlyInTerritories = true;
    }
}
