using AlliancesPlugin.Alliances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Integrations
{
    public class AllianceIntegrationCore
    {
        public static Guid GetAllianceId(string factionTag)
        {
            var alliance = AlliancePlugin.GetAllianceNoLoading(factionTag);
            if (alliance == null)
            {
                alliance = AlliancePlugin.GetAlliance(factionTag);
                if (alliance == null)
                {
                    return Guid.Empty;
                }
            }
            return alliance.AllianceId;
        }

        public static Alliance GetAllianceObj(string factionTag)
        {
            var alliance = AlliancePlugin.GetAllianceNoLoading(factionTag);
            if (alliance == null)
            {
                alliance = AlliancePlugin.GetAlliance(factionTag);
                if (alliance == null)
                {
                    return null;
                }
            }
            return alliance;
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
    }
}
