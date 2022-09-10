using System.Collections.Generic;
using ProtoBuf;
using VRageMath;

namespace Crunch
{
    [ProtoContract]
    public class PlayerDataPvP
    {
        [ProtoMember(1)] 
        public List<PvPArea> PvPAreas;
    }
    [ProtoContract]
    public class PvPArea
    {
        [ProtoMember(1)]
        public Vector3D Position;

        [ProtoMember(2)]
        public float Distance;

        [ProtoMember(3)]
        public string Name;

        [ProtoMember(4)]
        public bool AreaForcesPvP;
    }

    [ProtoContract]
    public class BoolStatus
    {
        [ProtoMember(1)]
        public bool Enabled;
    }
	
    [ProtoContract]
    public class ModMessage
    {
        [ProtoMember(1)]
        public string Type;

        [ProtoMember(2)]
        public byte[] Member;
    }
	
	[ProtoContract]
    public class DataRequest
    {
        [ProtoMember(1)]
        public ulong SteamId;

        [ProtoMember(2)] 
        public string DataType;
    }
}
