using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using VRageMath;

namespace AlliancesPlugin.KamikazeTerritories
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
