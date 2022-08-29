using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Alliances.Radar
{
    public class RadarConfig
    {
        public bool Enabled = false;
        public List<RadarModel> RadarBlocks = new List<RadarModel>();
        public Dictionary<string, float> BlocksThatLowerRadarDetectionChance = new Dictionary<string, float>();
    }
}
