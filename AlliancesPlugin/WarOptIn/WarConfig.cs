using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.WarOptIn
{
    public class WarConfig
    {
        public Boolean EnableOptionalWar = false;
        public long EnableWarCost = 1;
        public long DisableWarCost = 500000000;
        public float RefineryYieldMultiplierIfEnabled = 0f;
        public float AssemblerSpeedMultiplierIfEnabled = 0f;
        public float RefineryYieldMultiplierIfDisabled = 0f;
        public float AssemblerSpeedMultiplierIfDisabled = 0f;
    }
}
