using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AlliancesPlugin.Alliances;
using AlliancesPlugin.Shipyard;
using AlliancesPlugin.Territory_Version_2.Interfaces;
using AlliancesPlugin.Territory_Version_2.Models;
using AlliancesPlugin.Territory_Version_2.PointOwners;
using Newtonsoft.Json;
using Sandbox.Game.World;
using VRageMath;

namespace AlliancesPlugin.Territory_Version_2
{
    public static class CaptureHandler
    {
        public static async Task DoCaps()
        {
            List<Territory> TerritoriesToRecalc = new List<Territory>();
            foreach (var territory in AlliancePlugin.Territories)
            {
                foreach (var point in territory.Value.CapturePoints)
                {
                    ICapLogic CapLogic;

                    CapLogic = point;

                    try
                    {
                        var capResult = await CapLogic.ProcessCap(point, territory.Value);
                        if (capResult.Item1 && capResult.Item2 != null)
                        {
                            if (!TerritoriesToRecalc.Contains(territory.Value))
                            {
                                TerritoriesToRecalc.Add(territory.Value);
                            }
                            //  AlliancePlugin.Log.Info("Cap did succeed");
                        }
                        else
                        {
                            // AlliancePlugin.Log.Info("Cap did not succeed");
                        }
                    }
                    catch (Exception e)
                    {
                        AlliancePlugin.Log.Error($"Error on capture logic loop of type {CapLogic.GetType()}, { e.ToString()}");
                    }
                    //mostly testing, i dont intend to do anything here if a cap is or isnt successful, other than change the territory owner if % is high enough 

                    if (CapLogic.SecondaryLogics == null) continue;

                    foreach (var item in CapLogic.SecondaryLogics.OrderBy(x => x.Priority))
                    {
                        try
                        {
                            var result = await item.DoSecondaryLogic(CapLogic, territory.Value);
                            if (!result)
                            {
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            AlliancePlugin.Log.Error($"Error on secondary logic loop of type {item.GetType()} { e.ToString()}");
                        }
                    }

                }

                foreach (var ter in TerritoriesToRecalc.Distinct())
                {
                    var temp = new Dictionary<Object, int>();
                    foreach (var point in ter.CapturePoints)
                    {
                        switch (point.PointOwner)
                        {
                            case AlliancePointOwner alliance when temp.ContainsKey(alliance.AllianceId):
                                temp[alliance.AllianceId] += 1;
                                break;
                            case AlliancePointOwner alliance:
                                temp.Add(alliance.AllianceId, 1);
                                break;
                            case FactionPointOwner faction when temp.ContainsKey(faction.FactionId):
                                temp[faction.FactionId] += 1;
                                break;
                            case FactionPointOwner faction:
                                temp.Add(faction.FactionId, 1);
                                break;
                        }
                    }

                    if (temp.Any())
                    {
                        var max = temp.OrderByDescending(x => x.Value).First();

                        var ownedPercent = max.Value / ter.CapturePoints.Count * 100;
                        if (ownedPercent < ter.PercentRequiredToOwn)
                        {
                            ter.Owner = null;
                            continue;
                            //fail message
                        }
                        switch (max.Key)
                        {
                            case Guid id:
                                await TransferOwnershipToAlliance(id, ter);
                                break;
                            case long facId:
                                await TransferOwnershipToFaction(facId, ter);
                                break;
                        }
                    }

                    //recalc ownership here
                }
            }
        }

        public static async Task TransferOwnershipToAlliance(Guid allianceId, Territory ter)
        {

            if (allianceId == Guid.Empty)
            {
                SendMessage("Territory has been captured.", $"{ter.Name} captured by the {AlliancePlugin.config.PrefixName} Unknown alliance.", ter, ter.Owner);
                ter.Owner = new AlliancePointOwner()
                {
                    AllianceId = Guid.Empty
                };
                return;
            }
            var alliance = AlliancePlugin.GetAlliance(allianceId);
            SendMessage("Territory has been captured.", $"{ter.Name} captured by the {AlliancePlugin.config.PrefixName} {alliance.name}.", ter, ter.Owner);
            ter.Owner = new AlliancePointOwner()
            {
                AllianceId = allianceId
            };
        }

        public static async Task TransferOwnershipToFaction(long factionId, Territory ter)
        {
            var faction = MySession.Static.Factions.TryGetFactionById(factionId);
            SendMessage("Territory has been captured.", $"{ter.Name} captured by the faction {faction.Name}.", ter, ter.Owner);
            ter.Owner = new FactionPointOwner()
            {
                FactionId = factionId
            };
        }


        public static void SendMessage(string author, string message, Territory ter, IPointOwner owner)
        {
            ShipyardCommands.SendMessage(author, message, Color.Pink, 0l);
            var client = new WebClient();
            client.Headers.Add("Content-Type", "application/json");
            //send to ingame and nexus 
            var payloadJson = JsonConvert.SerializeObject(new
            {
                username = author,
                embeds = new[]
                    {
                        new
                        {
                            description = message,
                            title = author,
                            color = ter.EmbedColorString,
                        }
                    }
            }
            );

            var payload = payloadJson;
            try
            {
                client.UploadData(ter.DiscordWebhook, Encoding.UTF8.GetBytes(payload));
            }
            catch (Exception e)
            {
                AlliancePlugin.Log.Error($"Alliance Grid Cap Discord webhook error, {e}");
            }

            if (owner == null) return;

            try
            {
                var alliance = owner.GetOwner();
                if (alliance == null) return;
                var temp = alliance as Alliance;
                if (temp.DiscordWebhookCaps != "")
                {
                    client.UploadData(temp.DiscordWebhookCaps, Encoding.UTF8.GetBytes(payload));
                }
            }
            catch (Exception e)
            {
                AlliancePlugin.Log.Error($"Alliance Discord webhook error, {e}");
            }

        }

    }
}
