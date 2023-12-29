using System;
using ProtoBuf;

namespace CrunchGroup.Models.Events
{
    [ProtoContract]
    public class InvitedToGroupEvent
    {
        [ProtoMember(1)]
        public Guid GroupId { get; set; }
        [ProtoMember(2)]
        public long FactionId { get; set; }
    }
}
