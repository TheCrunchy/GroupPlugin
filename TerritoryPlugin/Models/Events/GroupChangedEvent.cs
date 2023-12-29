using ProtoBuf;

namespace CrunchGroup.Models.Events
{
    [ProtoContract]
    public class GroupChangedEvent
    {
        [ProtoMember(1)]
        public string Group { get; set; }
    }
}
