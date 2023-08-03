using AlliancesPlugin.Alliances.NewTerritories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.NewTerritoryCapture;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using VRage.Game;
using VRageMath;

namespace AlliancesPlugin.Alliances.NewTerritories
{
    public class Territory
    {
        public IPointOwner Owner;
        public double PercentOwned = 1;
        public double PercentRequiredToOwn = 0.75;
        public string WorldName = "default";
        public Guid Id = System.Guid.NewGuid();
        public string Name = "Example";
        public int Radius = 50000;
        public bool Enabled = false;
        public bool ForcesPvP = true;
        public List<ICapLogic> CapturePoints = new List<ICapLogic>();
        public string EntryMessage = "You are in {name} Territory";
        public string ControlledMessage = "Controlled by {alliance}";
        public string ExitMessage = "You have left {name} Territory";
        public Vector3D Position;
        public string DiscordWebhook = "https://discord.com/api/webhooks/1110180136118132827/DpKhjeIFUxwJqw8r1piKs0fnJ4HZCg4EcHiSCvzlHT0szKptgSoZNVHym7KdN8FjxKbc";
        public string EmbedColorString = "5763719";

    }
}
