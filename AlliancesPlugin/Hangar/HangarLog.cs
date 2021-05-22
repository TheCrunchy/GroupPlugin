using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin
{
   public class HangarLog
    {
        public Guid allianceId;
        public List<HangarLogItem> log = new List<HangarLogItem>();
    }
}
