using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Territory.Models.Events
{
    public class GroupDeletedEvent
    {
        public Guid GroupId { get; set; }
    }
}
