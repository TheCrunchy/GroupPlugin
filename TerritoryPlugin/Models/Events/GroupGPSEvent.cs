using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using VRageMath;

namespace CrunchGroup.Models.Events
{
    [ProtoContract]
    public class GroupGPSEvent
    {
        [ProtoMember(1)]
        public Guid GroupId { get; set; }
        [ProtoMember(2)]
        public ulong SenderId { get; set; }
        [ProtoMember(3)]
        public Vector3 Position { get; set; }
    }
}
