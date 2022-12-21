using AlliancesPlugin.Alliances.NewTerritories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRageMath;

namespace AlliancesPlugin.Alliances.NewTerritories
{
    public class Territory
    {
        public void StartSiege()
        {
            this.SiegeEndTime = DateTime.Now.AddHours(HoursSiegeLasts);
        }

        public string WorldName = "default";
        public Guid Id = System.Guid.NewGuid();
        public string Name = "Example";
        public int Radius = 50000;
        public bool Enabled = false;
        public Guid Alliance = Guid.Empty;
        public bool IsUnderSiege = false;
        public DateTime NextSiegeCheck;
        public DateTime SiegeEndTime;
        public int HoursSiegeLasts = 72;

        public bool ForcesPvP = true;
        public bool DisablesPvP = false;

        private Dictionary<Guid, int> AlliancePoints = new Dictionary<Guid, int>();

        public string EntryMessage = "You are in {name} Territory";
        public string ControlledMessage = "Controlled by {alliance}";
        public string ExitMessage = "You have left {name} Territory";
        public string OreTaxMessage = "You were taxed {amount} {oreName} by {alliance}";
        public double x;
        public double y;
        public double z;
        public Vector3D Position => new Vector3(x, y, z);
        public double CapX;
        public double CapY;
        public double CapZ;
        public Vector3D CapPosition => new Vector3(CapX, CapY, CapZ);
        //public float AssemblerSpeedBuff { get; set; } = 1.5f;
        //public float RefinerySpeedBuff { get; set; } = 1.5f;
        //public float RefineryYieldBuff { get; set; } = 1.5f;

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
