using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Territory.Models.Events
{
    public class NameChangedEvent
    {
        public Guid GroupId { get; set; }
        public string NewName { get; set; }
    }
}
