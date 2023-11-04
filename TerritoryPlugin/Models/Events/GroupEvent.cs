using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace Territory.Models.Events
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
