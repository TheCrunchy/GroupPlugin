using ProtoBuf;

namespace CrunchGroup.Models.Events
{
    [ProtoContract]
    public class GroupEvent
    {
        [ProtoMember(1)]
        public byte[] EventObject { get; set; }

        [ProtoMember(2)] 
        public string EventType { get; set; }
    }
}
