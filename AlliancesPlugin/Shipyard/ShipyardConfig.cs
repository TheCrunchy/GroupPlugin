using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin
{
    public class ShipyardConfig
    {
        public Boolean NearStartPointToClaim = true;
        public int MaxShipyardSlots = 1;
        public float SecondsPerBlockBase = 0.2f;
        public float StartingSpeedMultiply = 8f;
        public float MaxSpeedReduction = 1f;
        public Boolean SaveGameOnPrintStart = true;
        public Boolean ShowMoneyTakenOnStart = true;
        public int MaximumBlockSize = 10000;
        private List<ShipyardBlockConfig> blocks = new List<ShipyardBlockConfig>();

        public void AddToBlockConfig(ShipyardBlockConfig item)
        {
            if (!blocks.Contains(item))
            {
                blocks.Add(item);
            }
        }
        public void RemoveFromBlockConfig(ShipyardBlockConfig item)
        {
            if (blocks.Contains(item))
            {
                blocks.Remove(item);
            }
        }
        public List<ShipyardBlockConfig> GetBlocksConfig()
        {
            return blocks;
        }
        public ShipyardBlockConfig GetPrinterConfig(string subtype)
        {
            foreach (ShipyardBlockConfig conf in this.blocks)
            {
                if (conf.SubtypeId.Equals(subtype))
                {
                    return conf;
                }
            }
            return null;
        }
    }
}
