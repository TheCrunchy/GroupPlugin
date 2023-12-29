using System;
using ProtoBuf;

namespace CrunchGroup.Models.Events
{
    [ProtoContract]
    public class GroupDeletedEvent
    {
        [ProtoMember(1)]
        public Guid GroupId { get; set; }
    }
}
