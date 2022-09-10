using AlliancesPlugin.Alliances;
using AlliancesPlugin.Alliances.NewTerritories;
using AlliancesPlugin.KOTH;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;
using VRage.Game;
using VRageMath;
using static AlliancesPlugin.Alliances.NewTerritories.CityHandler;

namespace AlliancesPlugin.NewTerritories
{
    [PatchShim]
    public class SafezoneBlockPatch
    {
        internal static readonly MethodInfo update2 =
         typeof(MySafeZoneComponent).GetMethod("StartActivationCountdown", BindingFlags.Instance | BindingFlags.NonPublic) ??
         throw new Exception("Failed to find patch method");



        internal static readonly MethodInfo safezonePatch =
            typeof(SafezoneBlockPatch).GetMethod(nameof(SafezoneBlockPatchMethod), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");


        internal static readonly MethodInfo enabledUpdate =
     typeof(MyFunctionalBlock).GetMethod("OnEnabledChanged", BindingFlags.Instance | BindingFlags.NonPublic) ??
     throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo functionalBlockPatch =
            typeof(SafezoneBlockPatch).GetMethod(nameof(PatchTurningOn), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        public static bool PatchTurningOn(MyFunctionalBlock __instance)
        {
            // Log.Info("Button");
            if (AlliancePlugin.config == null || !AlliancePlugin.config.SafeZonesRequireTerritory)
            {
                return true;
            }
            MyFunctionalBlock block = __instance;
            if (block is Sandbox.ModAPI.IMyBeacon beacon)
            {

                MyFaction fac = MySession.Static.Factions.TryGetFactionByTag(beacon.GetOwnerFactionTag());
                if (fac == null)
                {
                    return true;
                }
                Alliance alliance = AlliancePlugin.GetAlliance(fac);
                if (alliance == null)
                {
                    return true;
                }
                if (beacon.Radius < 45001)
                {
                    return true;
                }
                var city = CityHandler.GetNearestCity(block.CubeGrid.PositionComp.GetPosition(), alliance);
                if (city != null)
                {
                    SiegeBeacon siege = new SiegeBeacon()
                    {
                        BeaconEntityId = beacon.EntityId,
                        TargetCity = city.CityId,
                        NextCycle = DateTime.Now.AddSeconds(60)
                    };
                    if (CityHandler.CanStartSiege(siege, city, beacon))
                    {
                        CityHandler.StartSiege(siege, city, beacon);
                    }
                    else
                    {
                        //do a fail message
                        return false;
                    }
                }
                return false;
            }

            return true;
        }

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(update2).Prefixes.Add(safezonePatch);
            ctx.GetPattern(enabledUpdate).Prefixes.Add(functionalBlockPatch);
        }
        public class TaxItem
        {
            public long playerId;
            public long price;
            public Guid territory;
        }
        public static void SendErrorMessageMaximumCities()
        {

        }

        public static void SendErrorMessageNotInRadius()
        {
            //   AlliancePlugin.Log.Info("Not in radius");
        }

        public static bool GenerateCity(MySafeZoneBlock SZBlock, Territory territory, Alliance alliance)
        {
            var template = CityHandler.CityTemplates.FirstOrDefault(x => x.SafeZoneSubTypeId == SZBlock.BlockDefinition.Id.SubtypeName);
            if (template == null)
            {
                //   AlliancePlugin.Log.Info("Null template");
                return false;
            }
            {
                City city = new City();
                city.AllianceId = alliance.AllianceId;
                city.CityRadius = template.CityRadius;
                city.CityType = template.CityType;
                city.CraftableItems = template.CraftableItems;
                city.DiscordChannelId = template.DiscordChannelId;
                city.EnableStationCrafting = template.EnableStationCrafting;
                city.GridId = SZBlock.Parent.EntityId;
                city.HasInit = false;
                city.nextCraftRefresh = DateTime.Now.AddSeconds(template.SecondsBeforeCityOperational);
                city.NextPayoutTime = DateTime.Now.AddSeconds(template.SecondsBeforeCityOperational);
                city.SafeZoneBlockId = SZBlock.EntityId;
                city.SecondsBetweenCrafting = template.SecondsBetweenCrafting;
                city.SecondsBetweenCreditPayout = template.SecondsBetweenCreditPayout;
                city.ShipyardSpeedBuffPercent = template.ShipyardSpeedBuffPercent;
                city.SiegePointsToDropSafeZone = template.SiegePointsToDropSafeZone;
                city.SpaceCreditsToCityOwners = template.SpaceCreditsToCityOwners;
                city.SpawnedItems = template.SpawnedItems;
                city.SecondsBeforeCityOperational = template.SecondsBeforeCityOperational;
                city.TimeCanInit = DateTime.Now.AddSeconds(template.SecondsBeforeCityOperational);
                city.WorldName = MyMultiplayer.Static.HostName;
                city.CityId = Guid.NewGuid();
                city.OwningTerritory = territory.Id;
                city.CityName = SZBlock.DisplayNameText;
            
                CityHandler.ActiveCities.Add(city);
                CityHandler.SendCityWillBeOperationalMessage(city);
               // AlliancePlugin.Log.Info("this shit should work");
                return true;
            }
        }

        public static bool SafezoneBlockPatchMethod(MySafeZoneComponent __instance)
        {
            if (AlliancePlugin.config == null || !AlliancePlugin.config.SafeZonesRequireTerritory)
            {
                return true;
            }
            MySafeZoneBlock SZBlock = __instance.Entity as MySafeZoneBlock;
            MyFaction fac = MySession.Static.Factions.TryGetFactionByTag(SZBlock.GetOwnerFactionTag());
            if (fac == null)
            {
                return false;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);

            if (alliance == null)
            {
            //    AlliancePlugin.Log.Info("alliance null");
                return false;
            }
            //  AlliancePlugin.Log.Info(SZBlock.Parent.GetType());
            foreach (Territory ter in AlliancePlugin.Territories.Values.Where(x => x.Alliance == alliance.AllianceId))
            {
              //  AlliancePlugin.Log.Info("territory");
                //location check   
                float distance = Vector3.Distance(__instance.Entity.PositionComp.GetPosition(), new Vector3(ter.x, ter.y, ter.z));
                if (distance <= ter.Radius)
                {
                    ////lets save a new city
                    if (GenerateCity(SZBlock, ter, alliance))
                    {
                        AlliancePlugin.utils.WriteToXmlFile<Territory>(AlliancePlugin.path + "//Territories//" + ter.Name + ".xml", ter);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    SendErrorMessageNotInRadius();
                    return false;
                }
            }

            return true;
            //wasnt one of those territories, now lets check if its in another that has no active cities
        }

    }
}
