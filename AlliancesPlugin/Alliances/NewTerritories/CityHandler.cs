using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace AlliancesPlugin.Alliances.NewTerritories
{
    public static class CityHandler
    {
        public static List<City> ActiveCities = new List<City>();
        public static List<SiegeBeacon> SiegeBeacons = new List<SiegeBeacon>();
        public async static Task HandleCitiesMain()
        {
            await HandleCitiesSiegeCycle();
            await Task.Run(async () =>
            {
                foreach (City city in ActiveCities)
                {
                    await HandleCitiesCraftingCycle(city);
                    await HandleCitiesItemSpawnCycle(city);
                  
                }
            });

        }

        public async static Task HandleCitiesSiegeCycle()
        {
            //invoke item shit on game thread?
            foreach (SiegeBeacon beacon in SiegeBeacons.Where(x => ActiveCities.Any(c => c.CityId == x.TargetCity)))
            {
                if (DateTime.Now >= beacon.NextCycle)
                {
                    //location checks before adding progress 
                    var beaconEnt = MyAPIGateway.Entities.GetEntityById(beacon.BeaconEntityId) as IMyBeacon;
                    if (beaconEnt != null)
                    {
                        if (!beaconEnt.Enabled || beaconEnt.Radius < 45000)
                        {
                            SendBeaconFailMessage(beacon);
                            continue;
                        }
                        City city = ActiveCities.FirstOrDefault(x => x.CityId == beacon.TargetCity);
                        if (CanBeaconAddSiegePoint(beacon, city, beaconEnt))
                        {
                            city.SiegeProgress += 1;
                            beacon.NextCycle = DateTime.Now.AddSeconds(60);
                        }
                    }
                    else
                    {
                        SendBeaconFailMessage(beacon);
                        continue;
                    }
                }
            }
        }

        public static void SendBeaconFailMessage(SiegeBeacon beacon)
        {

        }

        public static bool CanBeaconAddSiegePoint(SiegeBeacon beacon, City city, IMyBeacon beaconEnt)
        {
            bool CanAdd = true;

            MyCubeGrid cityGrid = MyAPIGateway.Entities.GetEntityById(city.GridId) as MyCubeGrid;

            if (cityGrid == null)
            {
                return false;
            }
            else
            {
                float distance = Vector3.Distance(cityGrid.PositionComp.GetPosition(), beaconEnt.CubeGrid.PositionComp.GetPosition());
                if (distance > 45000)
                {
                    CanAdd = false;
                }
            }

            foreach (SiegeBeacon otherBeacon in SiegeBeacons.Where(x => x.TargetCity == city.CityId))
            {
                var otherBeaconEnt = MyAPIGateway.Entities.GetEntityById(otherBeacon.BeaconEntityId) as IMyBeacon;
                if (otherBeaconEnt != null)
                {
                    float distance = Vector3.Distance(otherBeaconEnt.CubeGrid.PositionComp.GetPosition(), beaconEnt.CubeGrid.PositionComp.GetPosition());
                    if (distance < 30000)
                    {
                        CanAdd = false;
                    }
                }
            }

            return true;
        }

        public async static Task HandleCitiesCraftingCycle(City city)
        {
            //invoke item shit on game thread?
        }

        public async static Task HandleCitiesItemSpawnCycle(City city)
        {
            //invoke item shit on game thread?
        }

        public static int GetAdditionalShipyardSlots(Alliance alliance)
        {
            int additional = 0;
            foreach (City city in ActiveCities.Where(x => x.AllianceId == alliance.AllianceId))
            {
                additional += city.ShipyardSlotsProvided;
            }
            return additional;
        }

        public class SiegeBeacon
        {
            public long BeaconEntityId;
            public Guid TargetCity;
            public DateTime NextCycle;
        }

    }
}
