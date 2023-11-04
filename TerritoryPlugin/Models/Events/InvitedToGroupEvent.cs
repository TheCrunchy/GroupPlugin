using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Territory.Models.Events
{
    public class InvitedToGroupEvent
    {
        public Guid GroupId { get; set; }
        public long FactionId { get; set; }
    }
}
