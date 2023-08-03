using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances;
using AlliancesPlugin.Territory_Version_2.Interfaces;

namespace AlliancesPlugin.Territory_Version_2.PointOwners
{
    public class AlliancePointOwner : IPointOwner
    {
        public Guid AllianceId { get; set; }
        public object GetOwner()
        {
            var alliance = AlliancePlugin.GetAllianceNoLoading(AllianceId);
            return alliance;
        }
    }
}
