using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Alliances
{
    public class BankLogItem
    {
        public ulong SteamId;
        public long Amount;
        public DateTime TimeClaimed;
        public Boolean Claimed;
        public long BankAmount;
        public string Action;
        public ulong PlayerPaid = 0;
        public long FactionPaid = 0;
    }
}
