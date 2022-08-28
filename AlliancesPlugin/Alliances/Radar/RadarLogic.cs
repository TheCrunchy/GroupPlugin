using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities.Cube;

namespace AlliancesPlugin.Alliances.Radar
{
    public static class RadarLogic
    {
        public static RadarModel GetRadar(MyBeacon beacon)
        {
            return new RadarModel();
        }

        public static Dictionary<long, DateTime> RadarIntervals = new Dictionary<long, DateTime>();

        public static void DoRadar(MyBeacon beacon)
        {
            if (RadarIntervals.TryGetValue(beacon.EntityId, out var time))
            {
                if (DateTime.Now < time)
                {
                    return;
                }
            }
            var radar = GetRadar(beacon);
            if (radar.OnlyInTerritories)
            {
                DoRadarInTerritory(radar, beacon);
            }
            else
            {
                DoRadarNoTerritory(radar, beacon);
            }
        }

        public static void DoRadarNoTerritory(RadarModel radar, MyBeacon beacon)
        {

        }

        public static void DoRadarInTerritory(RadarModel radar, MyBeacon beacon)
        {

        }
    }
}
