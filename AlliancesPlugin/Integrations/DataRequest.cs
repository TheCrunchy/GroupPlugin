using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace AlliancesPlugin.Integrations
{
    [ProtoContract]
    public class DataRequest
    {
        [ProtoMember(1)]
        public ulong SteamId;

        [ProtoMember(2)] 
        public string DataType;
    }
}
