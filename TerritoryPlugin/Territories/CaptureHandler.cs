using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup.Handlers;
using CrunchGroup.Models;
using CrunchGroup.Models.Events;
using CrunchGroup.NexusStuff;
using CrunchGroup.Territories.CapLogics;
using CrunchGroup.Territories.Interfaces;
using CrunchGroup.Territories.PointOwners;
using Newtonsoft.Json;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace CrunchGroup.Territories
{
    public static class CaptureHandler
    {
        public static List<long> TrackedSafeZoneIds = new List<long>();

        public static List<Guid> SaveThese = new List<Guid>();

        public static Task DoCaps()
        {
            List<Guid> TerritoriesToRecalc = new List<Guid>();
            foreach (var territory in Core.Territories)
            {
                if (territory.Value.SecondaryLogics.Any())
                {
                    foreach (var item in territory.Value.SecondaryLogics.OrderBy(x => x.Priority))
                    {
                        try
                        {
                            var result = item.DoSecondaryLogic(new FactionGridCapLogic(), territory.Value);
                            if (!result.Result)
                            {
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Core.Log.Error($"Error on secondary logic loop of type {item.GetType()} { e.ToString()}");
                        }
                    }
                }
                foreach (var point in territory.Value.CapturePoints)
                {
                    ICapLogic CapLogic;

                    CapLogic = point;

                    try
                    {
                        var capResult = CapLogic.ProcessCap(point, territory.Value);
                        if (capResult.Result.Item1)
                        {
                            if (!TerritoriesToRecalc.Contains(territory.Value.Id))
                            {
                                TerritoriesToRecalc.Add(territory.Value.Id);
                            }
                            //  GroupPlugin.Log.Info("Cap did succeed");
                        }
                        else
                        {
                            // GroupPlugin.Log.Info("Cap did not succeed");
                        }
                    }
                    catch (Exception e)
                    {
                        Core.Log.Error($"Error on capture logic loop of type {CapLogic.GetType()}, { e.ToString()}");
                    }
                    //mostly testing, i dont intend to do anything here if a cap is or isnt successful, other than change the territory owner if % is high enough 

                    if (CapLogic.SecondaryLogics == null) continue;

                    foreach (var item in CapLogic.SecondaryLogics.OrderBy(x => x.Priority))
                    {
                        try
                        {
                            var result = item.DoSecondaryLogic(CapLogic, territory.Value);
                            if (!result.Result)
                            {
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Core.Log.Error($"Error on secondary logic loop of type {item.GetType()} { e.ToString()}");
                        }
                    }

                }
            }

            foreach (var ter in Core.Territories.Where(x => TerritoriesToRecalc.Contains(x.Value.Id)).Select(x => x.Value))
            {
                ter.RandomJsonStuff["OwnershipChanged"] = "true";
                var temp = new Dictionary<Object, int>();
                foreach (var point in ter.CapturePoints)
                {
                    switch (point.PointOwner)
                    {
                        case FactionPointOwner faction when temp.ContainsKey(faction.FactionId):
                            temp[faction.FactionId] += 1;

                            break;
                        case FactionPointOwner faction:
                            temp.Add(faction.FactionId, 1);
                            break;

                        case GroupPointOwner group when temp.ContainsKey(group.GroupId):
                            temp[group.GroupId] += 1;
                            break;
                        case GroupPointOwner group:
                            temp.Add(group.GroupId, 1);
                            break;
                    }
                }

                if (temp.Any())
                {
                    var max = temp.OrderByDescending(x => x.Value).First();
                    decimal ownedPercent = (decimal)max.Value / (decimal)ter.CapturePoints.Count;
              //      Core.Log.Info($"{ownedPercent}");
                    if (ownedPercent < (decimal) ter.PercentRequiredToOwn)
                    {
                        SendMessage("Territory has been lost.", $"Territory {ter.Name} is no longer owned.", ter, ter.Owner);
                        ter.Owner = null;
                        continue;
                        //fail message
                    }
                    switch (max.Key)
                    {
                        case long facId:
                            TransferOwnershipToFaction(facId, ter);
                            break;
                        case Guid groupId:
                            TransferOwnershipToGroup(groupId, ter);
                            break;
                    }
                }
                else
                {
                    SendMessage("Territory has been lost.", $"Territory {ter.Name} is no longer owned.", ter, ter.Owner);
                    ter.Owner = null;
                }

                //recalc ownership here
            }

            foreach (var territory in SaveThese.Distinct())
            {
                if (Core.Territories.TryGetValue(territory, out var ter))
                {
                    Task.Run(() =>
                    {
                        Core.utils.WriteToJsonFile<Territories.Models.Territory>(
                            Core.path + "//Territories//" + ter.Name + ".json", ter);
                    });
                }
            }

            return Task.CompletedTask;
        }

        public static Task TransferOwnershipToFaction(long factionId, Models.Territory ter)
        {
            var faction = MySession.Static.Factions.TryGetFactionById(factionId);
            SendMessage("Territory has been captured.", $"{ter.Name} captured by the faction {faction.Name}.", ter, ter.Owner);
            ter.Owner = new FactionPointOwner()
            {
                FactionId = factionId
            };
            Task.Run(() =>
            {
                Core.utils.WriteToJsonFile<Models.Territory>(Core.path + "//Territories//" + ter.Name + ".json", ter);
            });

            return Task.CompletedTask;
        }

        public static Task TransferOwnershipToGroup(Guid groupId, Models.Territory ter)
        {
            var group = GroupHandler.GetGroupById(groupId);
            SendMessage("Territory has been captured.", $"{ter.Name} captured by the {Core.PluginCommandPrefix} {group.GroupName}.", ter, ter.Owner);
            ter.Owner = new GroupPointOwner()
            {
                GroupId = groupId
            };
            Task.Run(() =>
            {
                Core.utils.WriteToJsonFile<Models.Territory>(Core.path + "//Territories//" + ter.Name + ".json", ter);
            });

            return Task.CompletedTask;
        }

        public static void SendRadarMessage(Object owner, String message)
        {
            var payloadJson = JsonConvert.SerializeObject(new
            {
                username = "Radar",
                embeds = new[]
                    {
                        new
                        {
                            description = message,
                            title = "Radar",
                            color = "15548997",
                        }
                    }
            }
            );

            var payload = payloadJson;

            var utf8 = Encoding.UTF8.GetBytes(payload);
            try
            {
                Task.Run(() =>
                {
                    switch (owner)
                    {
                        case IMyFaction faction:
                            {
                                Core.Log.Error($"Radar not implemented for factions");
                            }
                            break;
                        case Group group:
                            {
                                if (!string.IsNullOrWhiteSpace(group.DiscordWebhook))
                                {
                                    var client2 = new WebClient();
                                    client2.Headers.Add("Content-Type", "application/json");
                                    client2.UploadData(group.DiscordWebhook, utf8);
                                }
                            }
                            break;
                    }
                });
            }
            catch (Exception e)
            {
                Core.Log.Error($"Group Discord webhook error, {e}");
            }
        }

        public static void SendMessage(string author, string message, Models.Territory ter, IPointOwner owner)
        {
            Core.SendChatMessage(author, message, 0l);
            if (Core.NexusInstalled)
            {
                var Event = new GroupEvent();
                var createdEvent = new GlobalChatEvent()
                {
                    Author = author,
                    Message = message
                };
                Event.EventObject = MyAPIGateway.Utilities.SerializeToBinary(createdEvent);
                Event.EventType = createdEvent.GetType().Name;
                NexusHandler.RaiseEvent(Event);
            }

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
            Task.Run(() =>
            {
                var utf8 = Encoding.UTF8.GetBytes(payload);
                try
                {
                    if (!string.IsNullOrWhiteSpace(ter.DiscordWebhook) && ter.DiscordWebhook != "https://discord.com/api/webhooks/1110180136118132827/DpKhjeIFUxwJqw8r1piKs0fnJ4HZCg4EcHiSCvzlHT0szKptgSoZNVHym7KdN8FjxKbc")
                    {
                        client.UploadData(ter.DiscordWebhook, utf8);
                    }
                }
                catch (Exception e)
                {
                    Core.Log.Error($"Grid Cap Discord webhook error, {e}");
                }

                if (owner == null) return;

                try
                {
                    var ownerobj = owner.GetOwner();
                    if (ownerobj == null) return;
                    switch (ownerobj)
                    {
                        case Group group:
                            {
                                var temp = group;
                                if (!string.IsNullOrWhiteSpace(temp.DiscordWebhook))
                                {
                                    var client2 = new WebClient();
                                    client2.Headers.Add("Content-Type", "application/json");
                                    client2.UploadData(temp.DiscordWebhook, utf8);
                                }

                                break;
                            }
                    }

                }
                catch (Exception e)
                {
                    Core.Log.Error($"Group Discord webhook error, {e}");
                }
            });

        }

    }
}
