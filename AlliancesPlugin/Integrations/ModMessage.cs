using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace AlliancesPlugin.KamikazeTerritories
{
    [ProtoContract]
    public class ModMessage
    {
        [ProtoMember(1)]
        public string Type;

        [ProtoMember(2)]
        public byte[] Member;
    }
}
