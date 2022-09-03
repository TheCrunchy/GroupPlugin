using AlliancesPlugin.Alliances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRageMath;

namespace AlliancesPlugin.Integrations
{
    public static class AllianceIntegrationCore
    {
        public static Guid GetAllianceId(string factionTag)
        {
            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                return Guid.Empty;
            }
            var alliance = AlliancePlugin.GetAllianceNoLoading(faction);
            if (alliance != null) return alliance.AllianceId;
            alliance = AlliancePlugin.GetAlliance(faction);
            return alliance == null ? Guid.Empty : alliance.AllianceId;
        }

        public static Alliance GetAllianceObj(string factionTag)
        {
            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                return null;
            }
            var alliance = AlliancePlugin.GetAllianceNoLoading(faction);
            if (alliance != null) return alliance;
            alliance = AlliancePlugin.GetAlliance(faction);
            return alliance ?? null;
        }
        public static List<long> GetAllianceMembers(string factionTag)
        {
            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                return new List<long>();
            }
            var alliance = AlliancePlugin.GetAllianceNoLoading(faction);
            if (alliance != null) return new List<long>();
            alliance = AlliancePlugin.GetAlliance(faction);
            return alliance.AllianceMembers;
        }

        public static int GetMaximumForShipClassType(string factionTag, string classType)
        {
            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                return 0;
            }

            var alliance = AlliancePlugin.GetAllianceNoLoading(faction);
            if (alliance == null)
            {
                return 0;
            }
            alliance.LoadShipClassLimits();
            return alliance.GetShipClassLimit(classType);
        }

        public static bool DoesPlayHavePermission(long PlayerIdentityId, string Permission)
        {
            
            var fac = MySession.Static.Factions.GetPlayerFaction(PlayerIdentityId);
            if (fac == null)
            {
                return false;
            }
            else
            {
                var alliance = AlliancePlugin.GetAllianceNoLoading(fac);
                if (alliance == null)
                {
                    return false;
                }
                else
                {
                    if (!Enum.TryParse(Permission, out AccessLevel level)) return false;
                    var steamid = MySession.Static.Players.TryGetSteamId(PlayerIdentityId);
                    return alliance.HasAccess(steamid, level);

                }
            }

            return false;
        }

        public static double GetRefineryYieldMultiplier(long PlayerId, MyRefinery Refin)
        {
            return MyProductionPatch.GetRefineryYieldMultiplier(PlayerId, Refin);
        }

        public static double GetAssemblerSpeedMultiplier(long PlayerId, MyAssembler Assembler)
        {
            return MyProductionPatch.GetAssemblerSpeedMultiplier(PlayerId, Assembler);
        }
        public static double GetRefinerySpeedMultiplier(long PlayerId, MyAssembler Assembler)
        {
            return MyProductionPatch.GetAssemblerSpeedMultiplier(PlayerId, Assembler);
        }

        public static void ReceiveModMessage(byte[] data)
        {
          //  AlliancePlugin.Log.Info("Received message");
           // var encasedData = MyAPIGateway.Utilities.SerializeFromBinary<String>(data);

           // AlliancePlugin.Log.Info(encasedData);

        }

    }
}
