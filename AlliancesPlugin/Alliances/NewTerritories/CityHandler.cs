using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.Network;
using VRage.ObjectBuilders;
using VRageMath;
using static AlliancesPlugin.Alliances.NewTerritories.City;

namespace AlliancesPlugin.Alliances.NewTerritories
{
    public static class CityHandler
    {
        public static List<City> ActiveCities = new List<City>();
        public static List<SiegeBeacon> SiegeBeacons = new List<SiegeBeacon>();
        public static MethodInfo enabledSafeZoneMethod;

        public static List<City> CityTemplates = new List<City>();

        public static List<Vector3> GetAllCityLocations(Alliance alliance)
        {
            List<Vector3> locations = new List<Vector3>();
            foreach (City city in ActiveCities.Where(x => x.AllianceId == alliance.AllianceId && x.HasInit))
            {
                MyCubeGrid grid = MyAPIGateway.Entities.GetEntityById(city.GridId) as MyCubeGrid;
                if (grid == null)
                {
                    continue;
                }
                locations.Add(grid.PositionComp.GetPosition());
            }

            return locations;
        }
        public static List<Vector3> GetAllCityLocations()
        {
            List<Vector3> locations = new List<Vector3>();
            foreach (City city in ActiveCities.Where(x => x.HasInit))
            {
                MyCubeGrid grid = MyAPIGateway.Entities.GetEntityById(city.GridId) as MyCubeGrid;
                if (grid == null)
                {
                    continue;
                }
                locations.Add(grid.PositionComp.GetPosition());
            }

            return locations;
        }

        public static void StartSiege(SiegeBeacon siege, City city, Sandbox.ModAPI.IMyBeacon beacon)
        {
            SiegeBeacons.Add(siege);
            SendStartSiegeMessage(city);
        }
        public static void SendStartSiegeMessage(City city)
        {
            // DiscordStuff.SendMessageToDiscord($"{city.CityType} now operational.");
            Alliance alliance = AlliancePlugin.GetAllianceNoLoading(city.AllianceId);
            AlliancePlugin.SendChatMessage("[City Alerts]", $"{city.CityName} Owned by {alliance.name} is now under siege. {city.SiegeProgress}/{city.SiegePointsToDropSafeZone}");
            if (city.DiscordChannelId > 0)
            {
                DiscordStuff.SendAllianceMessage(alliance, "[City Alerts]", $"{city.CityName} Owned by {alliance.name} is now under siege. {city.SiegeProgress}/{city.SiegePointsToDropSafeZone}");
            }
        }
        public static City GetNearestCity(Vector3 player, Alliance alliance)
        {

            foreach (City city in ActiveCities.Where(x => x.HasInit && x.AllianceId != alliance.AllianceId))
            {
                MyCubeGrid grid = MyAPIGateway.Entities.GetEntityById(city.GridId) as MyCubeGrid;
                if (grid == null)
                {
                    continue;
                }
                MySafeZoneBlock SZBlock = MyAPIGateway.Entities.GetEntityById(city.SafeZoneBlockId) as MySafeZoneBlock;
                if (SZBlock == null)
                {
                    // AlliancePlugin.Log.Info("SZ null");
                    continue;
                }

                foreach (var comp in SZBlock.Components)
                {
                    if (comp is MySafeZoneComponent SZComp)
                    {
                        if (SZComp.IsSafeZoneEnabled())
                        {
                            float distance = Vector3.Distance(player, SZBlock.PositionComp.GetPosition());
                            if (distance <= 45001)
                            {
                                return city;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static void DeleteInactiveCities()
        {
            List<City> YeetTheseCities = new List<City>();
            foreach (City city in ActiveCities.Where(x => x.HasInit && x.WorldName == MyMultiplayer.Static.HostName))
            {
                MySafeZoneBlock SZBlock = MyAPIGateway.Entities.GetEntityById(city.SafeZoneBlockId) as MySafeZoneBlock;
                if (SZBlock == null)
                {
                    // AlliancePlugin.Log.Info("SZ null");
                    YeetTheseCities.Add(city);
                    continue;
                }

                foreach (var comp in SZBlock.Components)
                {
                    if (comp is MySafeZoneComponent SZComp)
                    {
                        if (!SZComp.IsSafeZoneEnabled())
                        {
                            //      AlliancePlugin.Log.Info("SZ not enabled");
                            YeetTheseCities.Add(city);
                            continue;
                        }
                    }
                }
                if (!SZBlock.Enabled)
                {
                    YeetTheseCities.Add(city);
                    continue;
                }
            }
            foreach (City city in YeetTheseCities)
            {
                ActiveCities.Remove(city);
                Territory ter = AlliancePlugin.Territories.Values.FirstOrDefault(x => x.Id == city.OwningTerritory);
                if (ter != null)
                {
                    SendCityDisabledMessage(city);
                    ter.ActiveCities.Remove(city);
                    AlliancePlugin.utils.WriteToXmlFile<Territory>(AlliancePlugin.path + "//Territories//" + ter.Name + ".xml", ter);
                }

            }
        }

        public static DateTime NextSave = DateTime.Now;
        public static void HandleCitiesMain()
        {
            //   AlliancePlugin.Log.Info(ActiveCities.Count + " count");
            DeleteInactiveCities();
            HandleCitiesSiegeCycle();
            HandleCityInitSZ();

            foreach (City city in ActiveCities.Where(x => x.WorldName == MyMultiplayer.Static.HostName))
            {
                HandleCitiesCraftingCycle(city);
                HandleCitiesItemSpawnCycle(city);
            }

            if (DateTime.Now >= NextSave)
            {
                NextSave = DateTime.Now.AddMinutes(1);
                foreach (Territory ter in AlliancePlugin.Territories.Values)
                {
                    AlliancePlugin.utils.WriteToXmlFile<Territory>(AlliancePlugin.path + "//Territories//" + ter.Name + ".xml", ter);
                }
            }

        }

        public static void HandleCityInitSZ()
        {

            // AlliancePlugin.Log.Info("City init shit ");
            foreach (City city in ActiveCities.Where(x => DateTime.Now >= x.TimeCanInit && !x.HasInit))
            {
                //    AlliancePlugin.Log.Info("Should be initing");
             //   AlliancePlugin.Log.Info(city.GridId);
                MyCubeGrid cityGrid = MyAPIGateway.Entities.GetEntityById(city.GridId) as MyCubeGrid;

                if (cityGrid == null)
                {
                    //  AlliancePlugin.Log.Info("grid null");
                    return;
                }
                MySafeZoneBlock SZ = MyAPIGateway.Entities.GetEntityById(city.SafeZoneBlockId) as MySafeZoneBlock;
                if (SZ == null)
                {
                    //  AlliancePlugin.Log.Info("SZ null");
                    return;
                }
                city.HasInit = true;
                //var index = ActiveCities.FindIndex(x => x.CityId == city.CityId);
                //ActiveCities[index] = city;
                SendCityOperationalMessage(city);
                Territory ter = AlliancePlugin.Territories[city.OwningTerritory];
                if (ter != null)
                {
                    AlliancePlugin.utils.WriteToXmlFile<Territory>(AlliancePlugin.path + "//Territories//" + ter.Name + ".xml", ter);
                }
            }
        }
        public static void SendCityWillBeOperationalMessage(City city)
        {
            // DiscordStuff.SendMessageToDiscord($"{city.CityType} now operational.");
            Alliance alliance = AlliancePlugin.GetAllianceNoLoading(city.AllianceId);
            AlliancePlugin.SendChatMessage("[City Alerts]", $"{city.CityName} will be operational in {city.SecondsBeforeCityOperational} seconds. Owned by {alliance.name}");
            if (city.DiscordChannelId > 0)
            {
                DiscordStuff.SendAllianceMessage(alliance, "[City Alerts]", $"{city.CityName} will be operational in {city.SecondsBeforeCityOperational} seconds. Owned by {alliance.name}");
            }
        }
        public static void SendCityDisabledMessage(City city)
        {
            // DiscordStuff.SendMessageToDiscord($"{city.CityType} now operational.");
            Alliance alliance = AlliancePlugin.GetAllianceNoLoading(city.AllianceId);
            AlliancePlugin.SendChatMessage("[City Alerts]", $"{city.CityName} Owned by {alliance.name} is now disabled");
            if (city.DiscordChannelId > 0)
            {
                DiscordStuff.SendAllianceMessage(alliance, "[City Alerts]", $"{city.CityName} Owned by {alliance.name} is now disabled");
            }
        }

        public static void SendCityOperationalMessage(City city)
        {
            // DiscordStuff.SendMessageToDiscord($"{city.CityType} now operational.");
            Alliance alliance = AlliancePlugin.GetAllianceNoLoading(city.AllianceId);
            AlliancePlugin.SendChatMessage("[City Alerts]", $"{city.CityName} now operational. Owned by {alliance.name}");
            if (city.DiscordChannelId > 0)
            {
                DiscordStuff.SendAllianceMessage(alliance, "[City Alerts]", $"{city.CityName} now operational. Owned by {alliance.name}");
            }
        }

        public static void SendSiegeProgress(City city)
        {
            // DiscordStuff.SendMessageToDiscord($"{city.CityType} now operational.");
            Alliance alliance = AlliancePlugin.GetAllianceNoLoading(city.AllianceId);
            AlliancePlugin.SendChatMessage("[City Alerts]", $"{city.CityName} {city.SiegeProgress}/{city.SiegePointsToDropSafeZone}");
            if (city.DiscordChannelId > 0)
            {
                DiscordStuff.SendAllianceMessage(alliance, "[City Alerts]", $"{city.CityName} {city.SiegeProgress}/{city.SiegePointsToDropSafeZone}");
            }
        }

        public static void SendSiegeSuccess(City city)
        {
            // DiscordStuff.SendMessageToDiscord($"{city.CityType} now operational.");
            Alliance alliance = AlliancePlugin.GetAllianceNoLoading(city.AllianceId);
            AlliancePlugin.SendChatMessage("[City Alerts]", $"{city.CityName} has fallen! Safezone disabled.");
            if (city.DiscordChannelId > 0)
            {
                DiscordStuff.SendAllianceMessage(alliance, "[City Alerts]", $"{city.CityName} has fallen! Safezone disabled.");
            }
        }
        public static void HandleCitiesSiegeCycle()
        {
            //invoke item shit on game thread?

            List<SiegeBeacon> failedBeacons = new List<SiegeBeacon>();
            foreach (SiegeBeacon beacon in SiegeBeacons.Where(x => ActiveCities.Any(c => c.CityId == x.TargetCity)))
            {
                if (DateTime.Now >= beacon.NextCycle)
                {
                    //location checks before adding progress 
                    var beaconEnt = MyAPIGateway.Entities.GetEntityById(beacon.BeaconEntityId) as Sandbox.ModAPI.IMyBeacon;
                    if (beaconEnt != null)
                    {
                        if (!beaconEnt.Enabled || beaconEnt.Radius < 45000)
                        {
                            SendBeaconFailMessage(beacon);
                            failedBeacons.Add(beacon);
                            continue;
                        }
                        City city = ActiveCities.FirstOrDefault(x => x.CityId == beacon.TargetCity);
                        if (CanBeaconAddSiegePoint(beacon, city, beaconEnt))
                        {
                            city.SiegeProgress += 1;
                            beacon.NextCycle = DateTime.Now.AddSeconds(60);
                            if (city.SiegeProgress >= city.SiegePointsToDropSafeZone)
                            {
                                MySafeZoneBlock SZ = MyAPIGateway.Entities.GetEntityById(city.SafeZoneBlockId) as MySafeZoneBlock;
                                if (SZ == null)
                                {
                                    return;
                                }
                                SZ.Enabled = false;
                                SendBeaconSuccessMessage(beacon);
                            }
                        }
                    }
                    else
                    {
                        SendBeaconFailMessage(beacon);
                        failedBeacons.Add(beacon);
                        continue;
                    }
                }
            }
            foreach (SiegeBeacon beacon in failedBeacons)
            {
                SiegeBeacons.Remove(beacon);
            }
        }
        public static void SendBeaconSuccessMessage(SiegeBeacon beacon)
        {

        }
        public static void SendBeaconFailMessage(SiegeBeacon beacon)
        {
            City city = ActiveCities.FirstOrDefault(x => x.CityId == beacon.TargetCity);
            // DiscordStuff.SendMessageToDiscord($"{city.CityType} now operational.");
            Alliance alliance = AlliancePlugin.GetAllianceNoLoading(city.AllianceId);
      
            AlliancePlugin.SendChatMessage("[City Alerts]", $"{city.CityName} beacon destroyed.");
            if (city.DiscordChannelId > 0)
            {
                DiscordStuff.SendAllianceMessage(alliance, "[City Alerts]", $"{city.CityName} has fallen! Safezone disabled.");
            }

        }

        public static bool CanStartSiege(SiegeBeacon beacon, City city, Sandbox.ModAPI.IMyBeacon beaconEnt)
        {
            bool CanStart = true;
            if (beaconEnt.BlockDefinition.SubtypeId != city.SiegeBeaconSubTypeId)
            {
                return false;
            }
            MyCubeGrid cityGrid = MyAPIGateway.Entities.GetEntityById(city.GridId) as MyCubeGrid;

            if (cityGrid == null)
            {
                return false;
            }
            if (SiegeBeacons.Any(x => x.BeaconEntityId == beaconEnt.EntityId))
            {
                return false;
            }
            foreach (SiegeBeacon otherBeacon in SiegeBeacons.Where(x => x.TargetCity == city.CityId))
            {
                var otherBeaconEnt = MyAPIGateway.Entities.GetEntityById(otherBeacon.BeaconEntityId) as Sandbox.ModAPI.IMyBeacon;
                if (otherBeaconEnt != null)
                {
                    float distance = Vector3.Distance(otherBeaconEnt.CubeGrid.PositionComp.GetPosition(), beaconEnt.CubeGrid.PositionComp.GetPosition());
                    if (distance < 30000)
                    {
                        CanStart = false;
                    }
                }
            }

            return CanStart;
        }

        public static bool CanBeaconAddSiegePoint(SiegeBeacon beacon, City city, Sandbox.ModAPI.IMyBeacon beaconEnt)
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
                var otherBeaconEnt = MyAPIGateway.Entities.GetEntityById(otherBeacon.BeaconEntityId) as Sandbox.ModAPI.IMyBeacon;
                if (otherBeaconEnt != null)
                {
                    float distance = Vector3.Distance(otherBeaconEnt.CubeGrid.PositionComp.GetPosition(), beaconEnt.CubeGrid.PositionComp.GetPosition());
                    if (distance < 30000)
                    {
                        CanAdd = false;
                    }
                }
            }

            return CanAdd;
        }
        public static List<VRage.Game.ModAPI.IMyInventory> GetInventories(MyCubeGrid grid)
        {
            List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();

            foreach (var block in grid.GetFatBlocks())
            {
                if (block is MyReactor reactor)
                {
                    continue;
                }
                for (int i = 0; i < block.InventoryCount; i++)
                {

                    VRage.Game.ModAPI.IMyInventory inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);
                    inventories.Add(inv);
                }

            }
            return inventories;
        }

        public static Random rnd = new Random();
        public static void HandleCitiesCraftingCycle(City city)
        {

            MyCubeGrid cityGrid = MyAPIGateway.Entities.GetEntityById(city.GridId) as MyCubeGrid;

            if (cityGrid == null)
            {
                // AlliancePlugin.Log.Info("Null city grid");
                return;
            }

            if (DateTime.Now >= city.nextCraftRefresh)
            {
                city.nextCraftRefresh = DateTime.Now.AddSeconds(city.SecondsBetweenCrafting);
                foreach (CraftedItem item in city.CraftableItems.Where(x => DateTime.Now >= x.NextCraftCycle))
                {
                    List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();
                    item.NextCraftCycle = DateTime.Now.AddSeconds(item.SecondsBetweenCycles);
                    // AlliancePlugin.Log.Info("1");
                    double yeet = rnd.NextDouble();
                    if (yeet <= item.ChanceToCraft)
                    {
                        //   AlliancePlugin.Log.Info("2");
                        var comps = new Dictionary<MyDefinitionId, int>();
                        inventories.AddRange(GetInventories(cityGrid));
                        long TotalCraftCost = 0;
                        if (MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId, item.SubtypeId, out MyDefinitionId id))
                        {
                            //    AlliancePlugin.Log.Info("3");
                            foreach (RecipeItem recipe in item.RequiredItems)
                            {
                                if (recipe.SpaceCreditsPerCraft > 0)
                                {
                                    if (EconUtils.getBalance(FacUtils.GetPlayersFaction(FacUtils.GetOwner(cityGrid)).FactionId) >= recipe.SpaceCreditsPerCraft)
                                    {
                                        long newTotal = TotalCraftCost + recipe.SpaceCreditsPerCraft;
                                        if (EconUtils.getBalance(FacUtils.GetPlayersFaction(FacUtils.GetOwner(cityGrid)).FactionId) < newTotal)
                                        {
                                            continue;
                                        }
                                        TotalCraftCost += recipe.SpaceCreditsPerCraft;
                                    }
                                }
                                if (MyDefinitionId.TryParse("MyObjectBuilder_" + recipe.TypeId, recipe.SubtypeId, out MyDefinitionId id2))
                                {
                                    comps.Add(id2, recipe.Amount);
                                }
                            }
                            if (ConsumeComponents(inventories, comps, 0l))
                            {
                                //      AlliancePlugin.Log.Info("4");
                                EconUtils.takeMoney(FacUtils.GetPlayersFaction(FacUtils.GetOwner(cityGrid)).FactionId, TotalCraftCost);
                                if (item.AmountPerCraft > 0)
                                {
                                    SpawnItems(cityGrid, id, item.AmountPerCraft);
                                }
                                if (item.SpaceCreditsPerCraft > 0)
                                {
                                    EconUtils.addMoney(FacUtils.GetPlayersFaction(FacUtils.GetOwner(cityGrid)).FactionId, item.SpaceCreditsPerCraft);
                                }
                                comps.Clear();
                                inventories.Clear();
                            }
                        }
                    }

                }
            }
        }
        public static void HandleCitiesSpaceCreditCycle(City city)
        {
            if (DateTime.Now >= city.NextPayoutTime)
            {
                MyCubeGrid cityGrid = MyAPIGateway.Entities.GetEntityById(city.GridId) as MyCubeGrid;

                if (cityGrid == null)
                {
                    return;
                }
                EconUtils.addMoney(FacUtils.GetPlayersFaction(FacUtils.GetOwner(cityGrid)).FactionId, city.SpaceCreditsToCityOwners);
                city.NextPayoutTime = DateTime.Now.AddSeconds(city.SecondsBetweenCreditPayout);
            }
        }
        public static void HandleCitiesItemSpawnCycle(City city)
        {
            //invoke item shit on game thread?

            MyCubeGrid cityGrid = MyAPIGateway.Entities.GetEntityById(city.GridId) as MyCubeGrid;

            if (cityGrid == null)
            {
                //   AlliancePlugin.Log.Info("Null city grid");
                return;
            }
            foreach (SpawnedItem item in city.SpawnedItems.Where(x => DateTime.Now >= x.NextSpawn))
            {
                item.NextSpawn = DateTime.Now.AddSeconds(item.SecondsBetweenSpawns);
                if (MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId, item.SubtypeId, out MyDefinitionId id))
                {
                    SpawnItems(cityGrid, id, item.AmountPerSpawn);
                }
            }
        }

        public static double GetAdditionalShipyardSpeed(Alliance alliance)
        {
            double additional = 0;
            foreach (City city in ActiveCities.Where(x => x.AllianceId == alliance.AllianceId))
            {
                additional += city.ShipyardSpeedBuffPercent;
            }
            return additional;
        }

        public class SiegeBeacon
        {
            public long BeaconEntityId;
            public Guid TargetCity;
            public DateTime NextCycle;
        }


        public static bool ConsumeComponents(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, IDictionary<MyDefinitionId, int> components, ulong steamid)
        {
            List<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, VRage.MyFixedPoint>> toRemove = new List<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, VRage.MyFixedPoint>>();
            foreach (KeyValuePair<MyDefinitionId, int> c in components)
            {
                MyFixedPoint needed = CountComponentsTwo(inventories, c.Key, c.Value, toRemove);
                if (needed > 0)
                {
                    return false;
                }
            }

            foreach (MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint> item in toRemove)
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    item.Item1.RemoveItemAmount(item.Item2, item.Item3);
                });
            return true;
        }
        public static MyFixedPoint CountComponentsTwo(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, MyDefinitionId id, int amount, ICollection<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>> items)
        {
            MyFixedPoint targetAmount = amount;
            foreach (VRage.Game.ModAPI.IMyInventory inv in inventories)
            {
                VRage.Game.ModAPI.IMyInventoryItem invItem = inv.FindItem(id);
                if (invItem != null)
                {
                    if (invItem.Amount >= targetAmount)
                    {
                        items.Add(new MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>(inv, invItem, targetAmount));
                        targetAmount = 0;
                        break;
                    }
                    else
                    {
                        items.Add(new MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>(inv, invItem, invItem.Amount));
                        targetAmount -= invItem.Amount;
                    }
                }
            }
            return targetAmount;
        }


        public static bool SpawnItems(MyCubeGrid grid, MyDefinitionId id, MyFixedPoint amount)
        {
            //  CrunchEconCore.Log.Info("SPAWNING 1 " + amount);
            if (grid != null)
            {

                //   CrunchEconCore.Log.Info("GRID NO NULL?");
                foreach (var block in grid.GetFatBlocks())
                {
                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        //    CrunchEconCore.Log.Info("SPAWNING 2 " + amount);
                        VRage.Game.ModAPI.IMyInventory inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);

                        MyItemType itemType = new MyInventoryItemFilter(id.TypeId + "/" + id.SubtypeName).ItemType;
                        if (inv.CanItemsBeAdded(amount, itemType))
                        {
                            inv.AddItems(amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
                            //      CrunchEconCore.Log.Info("SPAWNING 3 " + amount);
                            return true;
                        }
                        continue;

                    }
                }
                //   Log.Info("Should spawn item");


            }
            else
            {

            }
            return false;
        }

    }
}
