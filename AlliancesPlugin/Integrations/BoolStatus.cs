using ProtoBuf;

namespace AlliancesPlugin.Integrations
{
    [ProtoContract]
    public class BoolStatus
    {
        [ProtoMember(1)]
        public bool Enabled;
    }
}
