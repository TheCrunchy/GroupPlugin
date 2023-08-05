using ProtoBuf;
using VRageMath;

namespace AlliancesPlugin.Integrations
{
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
}
