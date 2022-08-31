using AlliancesPlugin.Alliances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;

namespace AlliancesPlugin.Integrations
{
    public static class AllianceIntegrationCore
    {
        public static Guid GetAllianceId(string factionTag)
        {
            var alliance = AlliancePlugin.GetAllianceNoLoading(factionTag);
            if (alliance != null) return alliance.AllianceId;
            alliance = AlliancePlugin.GetAlliance(factionTag);
            return alliance == null ? Guid.Empty : alliance.AllianceId;
        }

        public static Alliance GetAllianceObj(string factionTag)
        {
            var alliance = AlliancePlugin.GetAllianceNoLoading(factionTag);
            if (alliance != null) return alliance;
            alliance = AlliancePlugin.GetAlliance(factionTag);
            return alliance ?? null;
        }
        public static List<long> GetAllianceMembers(string factionTag)
        {
            var alliance = AlliancePlugin.GetAllianceNoLoading(factionTag);
            if (alliance != null) return new List<long>();
            alliance = AlliancePlugin.GetAlliance(factionTag);
            return alliance.AllianceMembers;
        }

        public static int GetMaximumForShipClassType(string factionTag, string classType)
        {
            var alliance = GetAllianceObj(factionTag);
            if (alliance == null)
            {
                return 0;
            }
            alliance.LoadShipClassLimits();
            return alliance.GetShipClassLimit(classType);
        }

        public static double GetRefineryYieldMultiplier(long PlayerId, MyRefinery Refin)
        {
            return MyProductionPatch.GetRefineryYieldMultiplier(PlayerId, Refin);
        }

        public static double GetAssemblerSpeedMultiplier(long PlayerId, MyAssembler Assembler)
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
