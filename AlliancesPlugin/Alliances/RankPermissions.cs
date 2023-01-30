using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Alliances
{
    public class RankPermissions
    {
        public List<AccessLevel> permissions { get; set; } = new List<AccessLevel>();
        public int permissionLevel { get; set; } = 100;
        public float taxRate { get; set; } = 0f;
        public string RankName { get; set; }
        public string Key { get; set; }
    }
}
