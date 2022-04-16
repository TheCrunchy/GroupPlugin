using AlliancesPlugin.Alliances;
using AlliancesPlugin.Alliances.NewTerritories;
using AlliancesPlugin.KOTH;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
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

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(update2).Prefixes.Add(safezonePatch);
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

        }

        public static bool GenerateCity(MySafeZoneBlock SZBlock, Territory territory, Alliance alliance)
        {
            var template = CityHandler.CityTemplates.FirstOrDefault(x => x.SafeZoneSubTypeId == SZBlock.BlockDefinition.Id.SubtypeName);
            if (template == null)
            {
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
                city.TimeCanInit = DateTime.Now.AddSeconds(template.SecondsBeforeCityOperational);
                city.WorldName = MyMultiplayer.Static.HostName;
                city.CityId = Guid.NewGuid();
                territory.ActiveCities.Add(city);
                CityHandler.ActiveCities.Add(city);
                return true;
            }
        }

        public static bool SafezoneBlockPatchMethod(MySafeZoneComponent __instance)
        {
            MySafeZoneBlock SZBlock = __instance.Entity as MySafeZoneBlock;
            Alliance alliance = AlliancePlugin.GetAlliance(SZBlock.GetOwnerFactionTag());

            if (alliance == null)
            {
                return false;
            }

            foreach (Territory ter in AlliancePlugin.Territories.Values.Where(x => x.Alliance == alliance.AllianceId))
            {
                //location check   
                float distance = Vector3.Distance(__instance.Entity.PositionComp.GetPosition(), new Vector3(ter.x, ter.y, ter.z));
                if (distance <= ter.Radius)
                {
                    if (ter.ActiveCities.Count > 4)
                    {
                        SendErrorMessageMaximumCities();
                        return false;
                    }
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
