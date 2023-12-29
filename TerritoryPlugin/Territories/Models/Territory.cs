using System;
using System.Collections.Generic;
using CrunchGroup.Territories.Interfaces;
using VRageMath;

namespace CrunchGroup.Territories.Models
{
    public class Territory
    {
        public IPointOwner Owner;
        public double PercentOwned = 1;
        public double PercentRequiredToOwn = 0.75;
        public string WorldName = "default";
        public Guid Id = System.Guid.NewGuid();
        public string Name = "Example";
        public bool Enabled = false;
        public List<ICapLogic> CapturePoints = new List<ICapLogic>();
        public Vector3D Position;
        public string DiscordWebhook = "https://discord.com/api/webhooks/1110180136118132827/DpKhjeIFUxwJqw8r1piKs0fnJ4HZCg4EcHiSCvzlHT0szKptgSoZNVHym7KdN8FjxKbc";
        public string EmbedColorString = "5763719";
        public List<ISecondaryLogic> SecondaryLogics = new List<ISecondaryLogic>();
    }
}
