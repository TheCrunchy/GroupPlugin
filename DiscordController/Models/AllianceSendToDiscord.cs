using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Alliances
{
    public class AllianceSendToDiscord
    {
        public string MessageText { get; set; }
        public string SenderPrefix { get; set; }
        public bool DoEmbed { get; set; } = false;
        public ulong ChannelId { get; set; }
        public string BotToken { get; set; }
        public int EmbedR { get; set; } = 100;
        public int EmbedG { get; set; } = 100;
        public int EmbedB { get; set; } = 100;
        public bool SendToIngame { get; set; } = true;
    }
}
