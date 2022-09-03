using System.Collections.Generic;
using ProtoBuf;
using VRageMath;

namespace Crunch
{
    [ProtoContract]
    public class PlayerDataPvP
    {
        [ProtoMember(1)]
        public bool WarEnabled;

        [ProtoMember(2)]
        public List<PvPArea> PvPAreas;
    }
    [ProtoContract]
    public class PvPArea
    {
        [ProtoMember(1)]
        public Vector3D Position;

        [ProtoMember(2)]
        public int Distance;

        [ProtoMember(3)]
        public string Name;

        [ProtoMember(4)]
        public bool AreaForcesPvP;
    }

    [ProtoContract]
    public class ChatStatus
    {
        [ProtoMember(1)]
        public bool ChatEnabled;
    }
}
