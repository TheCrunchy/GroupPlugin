using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.KOTH
{
    public class Territory
    {
        public Guid Id = System.Guid.NewGuid();
        public string Name;
        public int Radius = 50000;
        public bool enabled = true;
        public Guid Alliance = Guid.Empty;
        public string EntryMessage = "You are in {name} Territory";
        public string ControlledMessage = "Controlled by {alliance}";
        public string ExitMessage = "You have left {name} Territory";
        public double x;
        public double y;
        public double z;
    }
}
