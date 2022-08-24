using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Alliances
{
    public class AllianceChatMessage
    {
        public Guid AllianceId { get; set; }
        public string MessageText { get; set; }
        public string SenderPrefix { get; set; }
        public ulong ChannelId { get; set; }
        public string BotToken { get; set; }
        public bool FromDiscord { get; set; }
        public long SenderId { get; set; }
    }
}
