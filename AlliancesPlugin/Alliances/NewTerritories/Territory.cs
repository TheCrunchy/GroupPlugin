using AlliancesPlugin.Alliances.NewTerritories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.NewTerritoryCapture;
using AlliancesPlugin.NewTerritoryCapture.Models;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using VRage.Game;
using VRageMath;

namespace AlliancesPlugin.Alliances.NewTerritories
{
    public class Territory
    {
        public double PercentRequiredToOwn = 0.75;
        public string WorldName = "default";
        public Guid Id = System.Guid.NewGuid();
        public string Name = "Example";
        public int Radius = 50000;
        public bool Enabled = false;
        public IPointOwner Owner;

        public bool ForcesPvP = true;

        public List<ICapLogic> CapturePoints = new List<ICapLogic>();
        public string EntryMessage = "You are in {name} Territory";
        public string ControlledMessage = "Controlled by {alliance}";
        public string ExitMessage = "You have left {name} Territory";
        public double x;
        public double y;
        public double z;
        public Vector3D Position => new Vector3(x, y, z);
        public double CapX;
        public double CapY;
        public double CapZ;
        public Vector3D CapPosition => new Vector3(CapX, CapY, CapZ);

    }
}
