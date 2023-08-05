using ProtoBuf;

namespace AlliancesPlugin.Integrations
{
    [ProtoContract]
    public class ModMessage
    {
        [ProtoMember(1)]
        public string Type;

        [ProtoMember(2)]
        public byte[] Member;
    }
}
