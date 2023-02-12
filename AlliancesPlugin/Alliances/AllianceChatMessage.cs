using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace AlliancesPlugin.Alliances
{
    [ProtoContract]
    public class AllianceChatMessage
    {
        [ProtoMember(1)]
        public Guid AllianceId { get; set; }
        [ProtoMember(2)]
        public string MessageText { get; set; }
        [ProtoMember(3)]
        public string SenderPrefix { get; set; }
        [ProtoMember(4)]
        public ulong ChannelId { get; set; }
        [ProtoMember(5)]
        public string BotToken { get; set; }
        [ProtoMember(6)]
        public bool FromDiscord { get; set; }
        [ProtoMember(7)]
        public long SenderId { get; set; }
    }
}
