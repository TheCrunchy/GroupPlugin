using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Alliances
{
    public class AlliancePackage
    {
        public Dictionary<long, string> SteamIdsAndNames = new Dictionary<long, string>();
        public Dictionary<Guid, string> OtherAlliances = new Dictionary<Guid, string>();
        public Alliance AllianceData;
        public Guid EditId;

      
    }
}
