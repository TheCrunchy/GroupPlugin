using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace AlliancesPlugin.Integrations
{
    [ProtoContract]
    public class AllianceData
    {
        [ProtoMember(1)]
        public string AllianceName { get; set; }
        [ProtoMember(2)]
        public Guid AllianceId { get; set; }
        [ProtoMember(3)]
        public List<long> FactionIds = new List<long>();
    }
}
