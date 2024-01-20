using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace CrunchGroup.Models.Events
{
    [ProtoContract]
    public class GlobalChatEvent
    {
        [ProtoMember(1)]
        public string Author { get; set; }

        [ProtoMember(2)]
        public string Message { get; set; }

    }
}
