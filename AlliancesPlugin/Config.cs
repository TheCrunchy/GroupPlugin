﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin
{
    public class Config
    {
        public long PriceNewAlliance = 500000000;
        public string StoragePath = "default";

        public Boolean ShipyardEnabled = false;
        public Boolean HangarEnabled = false;
        public Boolean KothEnabled = false;
        public int MaxHangarSlots = 10;
        public Boolean JumpGatesEnabled = false;
        public int JumpGateMinimumOffset = 500;
        public int JumPGateMaximumOffset = 1000;
        public int MaximumGateFee = 10000000;
        public string DiscordBotToken = "bob";
        public long BaseUpkeepFee = 100000000;
        public float PercentPerFac = 0.10f;
        public long FeePerMember = 10000000;
        public long ShipyardUpkeep = 100000000;
        public long HangarUpkeep = 100000000;
    }
}
