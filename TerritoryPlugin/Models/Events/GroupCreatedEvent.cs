using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace Territory.Models.Events
{
    [ProtoContract]
    public class GroupCreatedEvent
    {
        [ProtoMember(1)]
        public string CreatedGroup { get; set; }
    }
}
