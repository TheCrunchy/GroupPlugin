using AlliancesPlugin.Alliances.NewTerritories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace AlliancesPlugin.Alliances.NewTerritories
{
    public class Territory
    {
        public Guid Id = System.Guid.NewGuid();
        public string Name = "Unnamed";
        public int Radius = 50000;
        public bool enabled = true;
        public Guid Alliance = Guid.Empty;

        public string EntryMessage = "You are in {name} Territory";
        public string ControlledMessage = "Controlled by {alliance}";
        public string ExitMessage = "You have left {name} Territory";
        public string OreTaxMessage = "You were taxed {amount} {oreName} by {alliance}";
        public double x;
        public double y;
        public double z;

        public float AssemblerSpeedBuff { get; set; } = 0.5f;
        public float RefinerySpeedBuff { get; set; } = 0.5f;
        public float RefineryYieldBuff { get; set; } = 0.5f;

        public bool DoOreTax = true;
        public float TaxPercent = 0.2f;

        public bool FindGridOnSetup = true; 
        public string LootBoxTerminalName = "LOOT BOX";

        public long StationGridId { get; set; }
        public bool TransferOwnershipOnCap = true;

        public List<TransferBlock> BlocksToTransferToNewOwner = new List<TransferBlock>();

        public class TransferBlock
        {
            public string TypeId { get; set; } = "MyRefinery";
            public string SubTypeId { get; set; } = "LargeRefinery";
            public MyOwnershipShareModeEnum ShareMode = MyOwnershipShareModeEnum.All;
        }

        public bool DoSafeZone = false;
        public int SafeZoneUpdateIntervalMinutes = 10;
    }
}
