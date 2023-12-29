using ProtoBuf;

namespace CrunchGroup.Models.Events
{
    [ProtoContract]
    public class GroupCreatedEvent
    {
        [ProtoMember(1)]
        public string CreatedGroup { get; set; }
    }
}
