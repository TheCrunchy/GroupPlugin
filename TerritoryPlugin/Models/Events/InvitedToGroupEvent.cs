using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace Territory.Models.Events
{
    [ProtoContract]
    public class InvitedToGroupEvent
    {
        [ProtoMember(1)]
        public Guid GroupId { get; set; }
        [ProtoMember(2)]
        public long FactionId { get; set; }
    }
}
