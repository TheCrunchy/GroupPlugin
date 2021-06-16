using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.ShipMarket
{
    public class MarketItem
    {
        public ulong SellerSteamId;
        public Guid ItemId = System.Guid.NewGuid();
        public long Price;
        
    }
}
