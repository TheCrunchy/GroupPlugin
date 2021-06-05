using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.KOTH
{
    public class DenialPoint
    {
        private Dictionary<string, Boolean> points = new Dictionary<string, Boolean>();

        public void RemoveCap(string name)
        {
            points.Remove(name);
            points.Add(name, false);
        }

        public void AddCap(string name)
        {
            points.Remove(name);
            points.Add(name, true);
        }

        public Boolean IsDenied()
        {
            foreach (KeyValuePair<string, Boolean> key in points)
            {
                if (key.Value)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
