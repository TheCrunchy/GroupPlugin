using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace AlliancesPlugin.JumpGates
{
    public class JumpGate
    {
        public Guid GateId = System.Guid.NewGuid();
        public Boolean UseSafeZones = true;
        public long SafeZoneEntityId = 1;
        public Boolean GeneratedZone2 = false;
        public Guid TargetGateId;
        public Vector3 Position;
        public string GateName;
        public Boolean Enabled = true;
        public int RadiusToJump = 75;
        private FileUtils utils = new FileUtils();
        public Guid OwnerAlliance;
        public string LinkedKoth = "";
        public long fee = 0;
        public long upkeep = 100000000;
        public Boolean CanBeRented = false;
        public int MetaPointRentCost = 100;
        public DateTime NextRentAvailable = DateTime.Now;
        public int DaysPerRent = 7;
        public void Save()
        {
            utils.WriteToXmlFile<JumpGate>(AlliancePlugin.path + "//JumpGates//" + GateId + ".xml", this);
        }
        public void Delete()
        {
            File.Delete(AlliancePlugin.path + "//JumpGates//" + GateId + ".xml");
        }
    }
}
