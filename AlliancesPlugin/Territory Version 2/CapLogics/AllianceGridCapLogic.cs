﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances;
using AlliancesPlugin.Alliances.NewTerritories;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using AlliancesPlugin.Territory_Version_2.PointOwners;
using Newtonsoft.Json;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRageMath;
using VRageRender.Voxels;

namespace AlliancesPlugin.Territory_Version_2.CapLogics
{
    public class AllianceGridCapLogic : ICapLogic
    {
        public string PointName = "Example name";
        public string GPSofPoint = "Put a gps here";
        public string GridOwnerTag = "SPRT";

        public int CaptureRadius = 5000;
        public List<ISecondaryLogic> SecondaryLogics { get; set; }
        public DateTime NextLoop { get; set; }
        public int SecondsBetweenLoops { get; set; } = 60;
        public int SuccessfulCapLockoutTime = 3600;

        public int PointsToTake = 1;

        public Dictionary<Guid, int> Points = new Dictionary<Guid, int>();
        public IPointOwner PointOwner { get; set; }
        public string DiscordWebhook = "https://discord.com/api/webhooks/1110180136118132827/DpKhjeIFUxwJqw8r1piKs0fnJ4HZCg4EcHiSCvzlHT0szKptgSoZNVHym7KdN8FjxKbc";
        public string EmbedColorString = "5763719";
        public Task<Tuple<bool, IPointOwner>> ProcessCap(ICapLogic point, Territory territory)
        {
            SendMessage("Test", "Test Message");
            if (CanLoop())
            {
                NextLoop = DateTime.Now.AddSeconds(SecondsBetweenLoops);
                //do capture logic for suits in alliances

                var gpspoint = GPSHelper.ScanChat(GPSofPoint);
                if (gpspoint == null)
                {
                    AlliancePlugin.Log.Info($"GPS string for point {PointName} is not in correct format");
                    return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                }
                var sphere = new BoundingSphereD(gpspoint.Coords, CaptureRadius * 2);

                var contested = false;
                var foundAlliances = new List<Guid>();
                foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                {
                    var fac = FacUtils.GetPlayersFaction(FacUtils.GetOwner(grid));
                    if (fac != null && fac.Tag.Equals(GridOwnerTag))
                    {
                        continue;
                    }

                    var alliance = AlliancePlugin.GetAllianceNoLoading(fac as MyFaction);
                    if (alliance == null)
                    {
                        contested = true;
                    }
                }

                if (!contested)
                {
                    contested = foundAlliances.Count > 1;
                }

                if (contested || !foundAlliances.Any())
                {
                    if (contested)
                    {
                        SendMessage($"Territory Capture", $"{PointName}, Contested, point not captured.");
                    }

                    return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                }
                else
                {
                    var owner = foundAlliances.First();
                    var currentPointOwner = PointOwner.GetOwner() as Alliance;
                    if (currentPointOwner.AllianceId == owner)
                    {
                        return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                    }
                    var pointOwner = new AlliancePointOwner()
                    {
                        AllianceId = owner
                    };
                    if (Points.TryGetValue(owner, out int currentPoints))
                    {
                        Points[owner] = currentPoints + 1;
                    }
                    else
                    {
                        Points.Add(owner, 1);
                    }
                    var hasPoints = Points[owner];
                    if (hasPoints < PointsToTake) return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
                    NextLoop = DateTime.Now.AddSeconds(SuccessfulCapLockoutTime);
                    PointOwner = pointOwner;
                    return Task.FromResult(Tuple.Create<bool, IPointOwner>(true, pointOwner));
                }
            }

            return Task.FromResult(Tuple.Create<bool, IPointOwner>(false, null));
        }

        public void SendMessage(string author, string message)
        {
            var client = new WebClient();
            client.Headers.Add("Content-Type", "application/json");

            var payloadJson = JsonConvert.SerializeObject(new
            {
                username = author,
                embeds = new[]
                {
                    new
                    {
                        description = message,
                        title = "Capture Message",
                        color = EmbedColorString,
                    }
                }
            }
            );

            var payload = payloadJson;
          
        client.UploadData(DiscordWebhook, Encoding.UTF8.GetBytes(payload));
        }

        public bool CanLoop()
        {
            return DateTime.Now >= NextLoop;
        }
    }
}