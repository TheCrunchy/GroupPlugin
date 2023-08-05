using System.Collections.Generic;
using ProtoBuf;

namespace AlliancesPlugin.Integrations
{
    [ProtoContract]
    public class PlayerDataPvP
    {
        [ProtoMember(1)] 
        public List<PvPArea> PvPAreas;
    }
}
