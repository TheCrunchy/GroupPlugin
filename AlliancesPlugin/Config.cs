using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin
{
    public class Config
    {
        public Boolean DisableJumpsWithId0 = true;
        public Boolean DoItemUpkeep = false;
        public long PriceNewAlliance = 500000000;
        public string StoragePath = "default";
        public Boolean AllowDiscord = true;
        public Boolean ShipyardEnabled = false;
        public Boolean MiningContractsEnabled = false;
        public int SecondsBetweenNewContracts = 300;
        public Boolean HangarEnabled = false;
        public Boolean KothEnabled = false;
        public int MaxHangarSlots = 10;
        public Boolean JumpGatesEnabled = false;
        public int MaxHangarSlotPCU = 60000;
        public int JumpGateMinimumOffset = 200;
        public int JumPGateMaximumOffset = 500;
        public int MaximumGateFee = 10000000;
        public string DiscordBotToken = "bob";
        public ulong DiscordChannelId = 1;
        public long BaseUpkeepFee = 100000000;
        public float PercentPerFac = 0.10f;
        public long FeePerMember = 10000000;
        public long ShipyardUpkeep = 100000000;
        public long HangarUpkeep = 100000000;
        public Boolean RepLogging = true;
        public Boolean ReputationPatch = true;
        public int UpkeepFailBeforeDelete = 3;

    }
}
