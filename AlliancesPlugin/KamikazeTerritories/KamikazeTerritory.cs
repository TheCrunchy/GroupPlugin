using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace AlliancesPlugin.KamikazeTerritories
{
    public class KamikazeTerritory
    {
        public long EntityId { get; set; }
        public Vector3D Position { get; set; }
        public float Radius { get; set; }
        public string Name { get; set; }
        public ClaimBlockSettings settings { get; set; }

        public bool ForcesPvP = true;
    }
}
