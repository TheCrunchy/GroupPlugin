using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Territory.Models.Events
{
    public class LeftGroupEvent
    {
        public Guid JoinedGroupId { get; set; }
        public long FactionId { get; set; }
    }
}
