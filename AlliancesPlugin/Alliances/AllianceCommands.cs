using AlliancesPlugin.Hangar;
using AlliancesPlugin.Shipyard;
using AlliancesPlugin.WarOptIn;
using Newtonsoft.Json;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AlliancesPlugin.KamikazeTerritories;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRageMath;
using RestSharp;
using Sandbox.Game.Entities.Cube;

namespace AlliancesPlugin.Alliances
{
    [Category("alliance")]
    public class AllianceCommands : CommandModule
    {
        public static Dictionary<long, DateTime> cooldowns = new Dictionary<long, DateTime>();

        public static string GetCooldownMessage(DateTime time)
        {
            var diff = time.Subtract(DateTime.Now);
            string output = String.Format("{0} Seconds", diff.Seconds) + " until command can be used.";
            return output;
        }

        [Command("adminaccept", "admin accept changes")]
        [Permission(MyPromoteLevel.Admin)]
        public void AllianceAcceptChangesAdmin()
        {
            var client = new RestClient($"{AlliancePlugin.config.EditorUrl}/api/alliance/GetAlliance");
            var request = new RestRequest();
            request.AddParameter("id", Context.Player.SteamUserId);
            Task.Run(async () =>
            {
                try
                {
                    var allianceResponse = await client.GetAsync(request);
                    if (allianceResponse.IsSuccessful &&
                        allianceResponse.StatusCode != HttpStatusCode.NotFound)
                    {

                        var temp = JsonConvert.DeserializeObject<string>(allianceResponse.Content);
                        var newAlliance = JsonConvert.DeserializeObject<Alliance>(temp);
                        var alliance = AlliancePlugin.GetAllianceNoLoading(newAlliance.AllianceId);
                        var tempToken = alliance.DiscordToken;
                        if (AlliancePlugin.AllAlliances.ContainsKey(newAlliance.name))
                        {
                            var check = AlliancePlugin.GetAllianceNoLoading(newAlliance.name);
                            if (check.AllianceId != newAlliance.AllianceId)
                            {
                                Context.Respond("Alliance name is in use, no changes have been saved.");
                                return;
                            }
                        }

                        alliance = newAlliance;

                        foreach (var tag in alliance.EditorKicks.Where(x => !string.IsNullOrWhiteSpace(x)))
                        {
                            var faction = MySession.Static.Factions.TryGetFactionByTag(tag.Trim());
                            if (faction == null)
                            {
                                continue;
                            }

                            AllianceChat.SendChatMessage(alliance.AllianceId, $"{AlliancePlugin.config.PrefixName}",
                                faction.Tag + $" was kicked from the {AlliancePlugin.config.PrefixName}!", true, 0);
                            alliance.AllianceMembers.Remove(faction.FactionId);
                            foreach (long id in alliance.AllianceMembers)
                            {
                                IMyFaction member = MySession.Static.Factions.TryGetFactionById(id);
                                if (member != null)
                                {
                                    MyFactionCollection.DeclareWar(member.FactionId, faction.FactionId);
                                    MySession.Static.Factions.SetReputationBetweenFactions(id,
                                        faction.FactionId, -1500);

                                    foreach (MyFactionMember m in member.Members.Values)
                                    {
                                        AllianceChat.PeopleInAllianceChat.Remove(
                                            MySession.Static.Players.TryGetSteamId(m.PlayerId));
                                    }
                                }
                            }
                        }

                        foreach (var facid in alliance.EditorInvites.Where(x => !string.IsNullOrWhiteSpace(x)))
                        {
                            var faction = MySession.Static.Factions.TryGetFactionByTag(facid.Trim());
                            if (faction != null)
                            {
                                var tempAlliance = AlliancePlugin.GetAllianceNoLoading(faction as MyFaction);
                                if (tempAlliance != null)
                                {
                                    AllianceChat.SendChatMessage(tempAlliance.AllianceId, $"{AlliancePlugin.config.PrefixName}",
                                        faction.Tag + $" was kicked from the {AlliancePlugin.config.PrefixName}!", true, 0);
                                    tempAlliance.AllianceMembers.Remove(faction.FactionId);
                                    foreach (long id in tempAlliance.AllianceMembers)
                                    {

                                        IMyFaction member = MySession.Static.Factions.TryGetFactionById(id);
                                        if (member != null)
                                        {
                                            MyFactionCollection.DeclareWar(member.FactionId, faction.FactionId);
                                            MySession.Static.Factions.SetReputationBetweenFactions(id,
                                                faction.FactionId, -1500);

                                            foreach (MyFactionMember m in member.Members.Values)
                                            {
                                                AllianceChat.PeopleInAllianceChat.Remove(
                                                    MySession.Static.Players.TryGetSteamId(m.PlayerId));
                                            }
                                        }
                                    }
                                }
                                alliance.ForceAddMember(faction.FactionId);
                            }

                        
                        }

                        alliance.EditorInvites = new List<string>();
                        alliance.EditorKicks = new List<string>();
                        alliance.DiscordToken = tempToken;
                        AlliancePlugin.SaveAllianceData(alliance);
                        Context.Respond("Changes saved!");
                    }
                    else
                    {
                        Context.Respond("Request was not successful, open a new editor and try again.");
                        return;
                    }
                }
                catch (Exception exception)
                {
                    AlliancePlugin.Log.Info(exception);
                    Context.Respond("Request timed out or could not connect");
                    return;
                }

            });
            Context.Respond("Request maybe worked");
        }

        [Command("loadall", "admin command for debug")]
        [Permission(MyPromoteLevel.Admin)]
        public void manualLoadAll()

        {
            Context.Respond(AlliancePlugin.KnownPaths.Count().ToString());
            AlliancePlugin.LoadAllAlliances();
        }

        [Command("modsend", "admin command for debug")]
        [Permission(MyPromoteLevel.Admin)]
        public void manualModSend()

        {
            AlliancePlugin.TriggerModUpdate();
        }

        [Command("accept", "accept changes")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceAcceptChanges()
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond($"Only factions can be in {AlliancePlugin.config.PrefixName}.");
                return;
            }

            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance == null)
            {
                Context.Respond($"You are not a member of an {AlliancePlugin.config.PrefixName}, or the id wasnt valid");
                return;
            }
            if (alliance.SupremeLeader == Context.Player.SteamUserId ||
                Context.Player.PromoteLevel == MyPromoteLevel.Admin)
            {

                Task.Run(async () =>
                {
                    utils.WriteToJsonFile($"{AlliancePlugin.path}//EditorBackups//{alliance.AllianceId}{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.json", alliance);
                    var client = new RestClient($"{AlliancePlugin.config.EditorUrl}/api/alliance/GetAlliance");
                    var request = new RestRequest();
                    request.AddParameter("id", Context.Player.SteamUserId);
                    try
                    {
                        var allianceResponse = await client.GetAsync(request);
                        if (allianceResponse.IsSuccessful && allianceResponse.StatusCode != HttpStatusCode.NotFound)
                        {
                            var temp = JsonConvert.DeserializeObject<string>(allianceResponse.Content);
                            var newAlliance = JsonConvert.DeserializeObject<Alliance>(temp);
                            var tempToken = alliance.DiscordToken;
                            var hasUnlockedShipyard = alliance.hasUnlockedShipyard;
                            var hasUnlockedHangar = alliance.hasUnlockedHangar;

                            if (AlliancePlugin.AllAlliances.ContainsKey(newAlliance.name))
                            {
                                var check = AlliancePlugin.GetAllianceNoLoading(newAlliance.name);
                                if (check.AllianceId != newAlliance.AllianceId)
                                {
                                    Context.Respond($"{AlliancePlugin.config.PrefixName} name is in use, no changes have been saved.");
                                    return;
                                }
                            }
                            alliance = newAlliance;
                            alliance.hasUnlockedHangar = hasUnlockedHangar;
                            alliance.hasUnlockedShipyard = hasUnlockedShipyard;
                            foreach (var tag in alliance.EditorKicks.Where(x => !string.IsNullOrWhiteSpace(x)))
                            {
                                var faction = MySession.Static.Factions.TryGetFactionByTag(tag.Trim());
                                if (faction == null)
                                {
                                    continue;
                                }
                                AllianceChat.SendChatMessage(alliance.AllianceId, $"{AlliancePlugin.config.PrefixName}",
                                    faction.Tag + $" was kicked from the {AlliancePlugin.config.PrefixName}!", true, 0);
                                alliance.AllianceMembers.Remove(faction.FactionId);
                                foreach (long id in alliance.AllianceMembers)
                                {

                                    IMyFaction member = MySession.Static.Factions.TryGetFactionById(id);
                                    if (member != null)
                                    {
                                        MyFactionCollection.DeclareWar(member.FactionId, faction.FactionId);
                                        MySession.Static.Factions.SetReputationBetweenFactions(id,
                                            faction.FactionId, -1500);

                                        foreach (MyFactionMember m in member.Members.Values)
                                        {
                                            AllianceChat.PeopleInAllianceChat.Remove(
                                                MySession.Static.Players.TryGetSteamId(m.PlayerId));
                                        }
                                    }
                                }
                            }
                            foreach (var tag in alliance.EditorInvites.Where(x => !string.IsNullOrWhiteSpace(x)))
                            {
                                var faction = MySession.Static.Factions.TryGetFactionByTag(tag.Trim());
                                if (faction == null)
                                {
                                    continue;
                                }
                                alliance.SendInvite(faction.FactionId);
                            }
                            alliance.DiscordToken = tempToken;
                            alliance.EditorInvites = new List<string>();
                            alliance.EditorKicks = new List<string>();
                            AlliancePlugin.SaveAllianceData(alliance);
                            Context.Respond("Changes saved!");
                        }
                        else
                        {
                            Context.Respond("Request was not successful, open a new editor and try again.");
                            return;
                        }
                    }
                    catch (Exception exception)
                    {
                        AlliancePlugin.Log.Info(exception);
                        Context.Respond("Request timed out or could not connect");
                        return;
                    }

                    Context.Respond("Request maybe worked");
                });

            }
            else
            {
                Context.Respond("You are not the leader or an admin! you cannot use this.");
            }
        }


        [Command("admineditor", "open the editor for admins")]
        [Permission(MyPromoteLevel.Admin)]
        public void AllianceOpenEditor(string AllianceName)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond($"Only factions can be in {AlliancePlugin.config.PrefixName}.");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(AllianceName);
            if (alliance == null)
            {
                Context.Respond($"You are not a member of an {AlliancePlugin.config.PrefixName}, or the id wasnt valid");
                return;
            }
            Task.Run(async () =>

         {
             alliance.DiscordToken = "Yeah im not sending this lmao";
             AlliancePackage alliancePackage = new AlliancePackage { AllianceData = alliance, EditId = Guid.NewGuid(), SteamId = Context.Player.SteamUserId };
             alliancePackage.ExpiresAt = DateTime.Now.AddHours(2);
             utils.WriteToJsonFile($"{AlliancePlugin.path}//EditorBackups//{alliance.AllianceId}{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.json", alliance);
             alliancePackage.FactionNames = alliance.GetMemberFactions();
             alliancePackage.SteamIdsAndNames = alliance.GetPlayerSteamIds();
             string allianceJson = JsonConvert.SerializeObject(alliancePackage);
             var client = new RestClient($"{AlliancePlugin.config.EditorUrl}/api/alliance/PostAlliance");

             var request = new RestRequest();
             request.AddStringBody(allianceJson, DataFormat.Json);
             //   var parameter = new BodyParameter("allianceJson", allianceJson, "application/json", DataFormat.Json);
             //   request.Parameters.AddParameter(parameter);

             try
             {

                 var result = await client.PostAsync(request);
                 if (result.IsSuccessful)
                 {
                     MyVisualScriptLogicProvider.OpenSteamOverlay(
                           $"https://steamcommunity.com/linkfilter/?url={AlliancePlugin.config.EditorUrl}/alliances/edit/" + alliancePackage.EditId.ToString(),
                           Context.Player.Identity.IdentityId);
                     Context.Respond("Opening?");
                 }
                 else
                 {
                     Context.Respond("Could not connect to server. try again later.");
                 }

             }
             catch (Exception e)
             {

                 throw;
             }
         });

        }

        [Command("editor", "open the editor")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceOpenEditor()
        {
            if (IsNotInAlliance(out var alliance)) return;
            Task.Run(async () =>
         {
             alliance.DiscordToken = "Yeah im not sending this lmao";
             AlliancePackage alliancePackage = new AlliancePackage { AllianceData = alliance, EditId = Guid.NewGuid(), SteamId = Context.Player.SteamUserId };
             alliancePackage.ExpiresAt = DateTime.Now.AddHours(2);
             alliancePackage.SteamIdsAndNames = alliance.GetPlayerSteamIds();
             alliancePackage.FactionNames = alliance.GetMemberFactions();
             string allianceJson = JsonConvert.SerializeObject(alliancePackage);
             var client = new RestClient($"{AlliancePlugin.config.EditorUrl}/api/alliance/PostAlliance");

             var request = new RestRequest();
             request.AddStringBody(allianceJson, DataFormat.Json);
             //   var parameter = new BodyParameter("allianceJson", allianceJson, "application/json", DataFormat.Json);
             //   request.Parameters.AddParameter(parameter);

             try
             {

                 var result = await client.PostAsync(request);
                 if (result.IsSuccessful)
                 {
                     MyVisualScriptLogicProvider.OpenSteamOverlay(
                           $"https://steamcommunity.com/linkfilter/?url={AlliancePlugin.config.EditorUrl}/alliances/edit/" + alliancePackage.EditId.ToString(),
                           Context.Player.Identity.IdentityId);
                     Context.Respond("Opening?");
                 }
                 else
                 {
                     Context.Respond("Could not connect to server. try again later.");
                 }

             }
             catch (Exception e)
             {

                 throw;
             }
         });
        }

        private bool IsNotInAlliance(out Alliance alliance)
        {
            alliance = null;
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond($"Only factions can be in {AlliancePlugin.config.PrefixName}.");
                return true;
            }

            alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance == null)
            {
                Context.Respond($"You are not a member of an {AlliancePlugin.config.PrefixName}, or the id wasnt valid");
                return true;
            }

            return false;
        }

        public class RequestBody
        {
            public string Name;
            public AlliancePackage Value;
        }

        [Command("token", "set a discord token")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceToken(string token)
        {
            if (IsNotInAlliance(out var alliance)) return;
            if (token.Length < 59)
            {
                Context.Respond("Token not a valid length!");
                return;
            }
            if (alliance != null)
            {
                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {
                    string encrypted = Encryption.EncryptString(alliance.AllianceId.ToString(), token);
                    alliance.DiscordToken = encrypted;
                    AlliancePlugin.SaveAllianceData(alliance);
                    Context.Respond("Added token! If the channel Id is also set, send an alliance message to start the bot");
                }
                else
                {
                    Context.Respond("Only the supreme leader can set the bot token.");
                }
            }
            else
            {
                Context.Respond("You dont have an alliance.");
            }
        }

        [Command("who", "see if a faction is in an alliance")]
        [Permission(MyPromoteLevel.None)]
        public void FindFactionAlliance(string factionTag)
        {
            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if (faction == null)
            {
                Context.Respond("Could not find a faction with that tag");
                return;
            }

            var alliance = AlliancePlugin.GetAlliance(faction);
            if (alliance == null)
            {
                Context.Respond("Faction is not a member of an alliance");
                return;
            }

            Context.Respond($"Faction is a member of {alliance.name}");
        }


        [Command("ter", "output loaded territories")]
        [Permission(MyPromoteLevel.Admin)]
        public void outputTerritories()
        {
            foreach (var ter in KamikazeTerritories.MessageHandler.Territories)
            {
                Context.Respond(ter.EntityId + "");
            }
            Context.Respond(KamikazeTerritories.MessageHandler.Territories.Count + " Loaded territories");
        }
        [Command("admintoken", "set a discord token with admin perms")]
        [Permission(MyPromoteLevel.Admin)]
        public void AllianceTokenAdmin(string allianceName, string token)
        {

            if (token.Length < 59)
            {
                Context.Respond("Token not a valid length!");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(allianceName);
            if (alliance != null)
            {

                string encrypted = Encryption.EncryptString(alliance.AllianceId.ToString(), token);
                alliance.DiscordToken = encrypted;
                AlliancePlugin.SaveAllianceData(alliance);

                Context.Respond("Added token! If the channel Id is also set the bot should start working after next server restart.");

            }
            else
            {
                Context.Respond("Alliance doesnt exist.");
            }
        }
        [Command("chatcolor", "change the alliance chat color")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceColor(int r, int g, int b)
        {
            if (IsNotInAlliance(out var alliance)) return;
            if (alliance != null)
            {
                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {
                    alliance.r = r;
                    alliance.g = g;
                    alliance.b = b;
                    AlliancePlugin.SaveAllianceData(alliance);

                    Context.Respond("Color updated!");
                }
                else
                {
                    Context.Respond("Only the supreme leader can set the bot token.");
                }
            }
            else
            {
                Context.Respond("You dont have an alliance.");
            }
        }
        [Command("adminchannel", "set a discord channel id")]
        [Permission(MyPromoteLevel.Admin)]
        public void AllianceChannelId(string allianceName, string channel)
        {


            Alliance alliance = AlliancePlugin.GetAlliance(allianceName);
            if (alliance != null)
            {


                try
                {
                    alliance.DiscordChannelId = ulong.Parse(channel);
                }
                catch (Exception)
                {
                    Context.Respond("Cannot parse that number.");
                    Context.Respond("Example usage - !alliance channel 785562535494549505");
                    return;
                }
                AlliancePlugin.SaveAllianceData(alliance);
                Context.Respond("Added Channel Id! If the token is also set the bot should start working after next server restart.");


            }
            else
            {
                Context.Respond("Couldnt find that alliance.");
            }
        }


        [Command("channel", "set a discord channel id")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceChannelId(string channel)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }

            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance != null)
            {
                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {

                    try
                    {
                        alliance.DiscordChannelId = ulong.Parse(channel);
                    }
                    catch (Exception)
                    {
                        Context.Respond("Cannot parse that number.");
                        Context.Respond("Example usage - !alliance channel 785562535494549505");
                        return;
                    }
                    AlliancePlugin.SaveAllianceData(alliance);
                    Context.Respond("Added Channel Id! If the token is also set the bot should start working after next server restart.");
                }
                else
                {
                    Context.Respond("Only the supreme leader can set the bot token.");
                }
            }
            else
            {
                Context.Respond("You dont have an alliance.");
            }
        }


        [Command("admindistress", "admindistress signals")]
        [Permission(MyPromoteLevel.Admin)]
        public void admindistress(string name, string messagename = "", string message = "")
        {


            if (Context.Player == null)
            {
                Context.Respond("no no console no distress");
                return;
            }
            if (messagename.Equals(""))
            {
                messagename = "Ship AI";
            }
            if (message.Equals(""))
            {
                message = "Distress signal detected. Unable to identify.";
            }

            MyGps gps = CreateGps(Context.Player.Character.PositionComp.GetPosition(), Color.Purple, 600, name, "");
            MyGps gpsRef = gps;
            long entityId = 0L;
            entityId = gps.EntityId;

            MyGpsCollection gpsCollection = (MyGpsCollection)MyAPIGateway.Session?.GPS;
            foreach (MyPlayer p in MySession.Static.Players.GetOnlinePlayers())
            {
                gpsCollection.SendAddGpsRequest(p.Identity.IdentityId, ref gpsRef, entityId, true);
                // NationsPlugin.signalsToClear.Add(gps, DateTime.Now.AddMilliseconds(NationsPlugin.file.MillisecondsTimeItLasts));
                ShipyardCommands.SendMessage(messagename, message, Color.Red, (long)p.Id.SteamId);
            }
        }
        private MyGps CreateGps(Vector3D Position, Color gpsColor, int seconds, String Nation, String Reason)
        {

            MyGps gps = new MyGps
            {
                Coords = Position,
                Name = Nation + " - Distress Signal ",
                DisplayName = Nation + " - Distress Signal ",
                GPSColor = gpsColor,
                IsContainerGPS = true,
                ShowOnHud = true,
                DiscardAt = new TimeSpan(0, 0, seconds, 0),
                Description = "Nation Distress Signal \n" + Reason,
            };
            gps.UpdateHash();


            return gps;
        }

        [Command("list", "list all alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceList()
        {
            StringBuilder sb = new StringBuilder();

            foreach (string name in AlliancePlugin.AllAlliances.Keys)
            {
                sb.AppendLine(name);
            }

            if (Context.Player != null)
            {
                DialogMessage m = new DialogMessage($"{AlliancePlugin.config.PrefixName} List", "", sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);

            }
            else
            {
                Context.Respond(sb.ToString());
            }
        }

        [Command("join", "join an alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceJoin(string name)
        {
            if (cooldowns.TryGetValue(Context.Player.IdentityId, out DateTime value))
            {
                if (DateTime.Now <= value)
                {
                    Context.Respond(GetCooldownMessage(value));
                    return;
                }
                else
                {
                    cooldowns.Remove(Context.Player.IdentityId);
                    cooldowns.Add(Context.Player.IdentityId, DateTime.Now.AddSeconds(60));
                }
            }
            else
            {
                cooldowns.Add(Context.Player.IdentityId, DateTime.Now.AddSeconds(60));
            }

            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (IsNotInAlliance(out var t)) return;
            if (t != null)
            {
                Context.Respond("You can only be in one alliance at a time!");
                return;
            }

            //name = Context.RawArgs;
            if (fac.IsLeader(Context.Player.IdentityId) || fac.IsFounder(Context.Player.IdentityId))
            {
                Alliance alliance = AlliancePlugin.GetAlliance(name);
                if (alliance != null)
                {
                    if (alliance.IsOpenToAll)
                    {
                        if (alliance.JoinAlliance(fac))
                        {
                            Context.Respond("Joined alliance!");

                            AlliancePlugin.FactionsInAlliances.Remove(fac.FactionId);
                            AlliancePlugin.FactionsInAlliances.Add(fac.FactionId, alliance.name);
                            AlliancePlugin.SaveAllianceData(alliance);
                            AllianceChat.SendChatMessage(alliance.AllianceId, "Alliance", fac.Tag + " has joined the alliance!", true, 0);
                            cooldowns.Remove(Context.Player.IdentityId);
                        }
                        else
                        {
                            Context.Respond("Couldnt join alliance. Your faction may have been banned.");
                        }
                        return;
                    }
                    if (alliance.Invites.Contains(fac.FactionId))
                    {
                        if (alliance.JoinAlliance(fac))
                        {
                            Context.Respond("Joined alliance!");

                            AlliancePlugin.FactionsInAlliances.Remove(fac.FactionId);
                            AlliancePlugin.FactionsInAlliances.Add(fac.FactionId, alliance.name);
                            AlliancePlugin.SaveAllianceData(alliance);
                            AllianceChat.SendChatMessage(alliance.AllianceId, "Alliance", fac.Tag + " has joined the alliance!", true, 0);
                            cooldowns.Remove(Context.Player.IdentityId);
                        }
                        else
                        {
                            Context.Respond("Couldnt join alliance. Your faction may have been banned.");
                        }
                    }
                }
                else
                {
                    Context.Respond("That alliance doesnt exist.");
                }
            }
            else
            {
                Context.Respond("Only leaders and founders can join an alliance.");
            }
        }
        [Command("description", "change the description")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceDescription(string description)
        {
            if (IsNotInAlliance(out var alliance)) return;

            if (alliance != null)
            {
                if (alliance.HasPermissionToInvite(Context.Player.SteamUserId))
                {
                    alliance.description = Context.RawArgs;
                    AlliancePlugin.SaveAllianceData(alliance);

                }
            }
        }
        public static AccessLevel StringToAccessLevel(string input)
        {
            switch (input.ToLower())
            {
                case "hangarsave":
                    return AccessLevel.HangarSave;
                case "hangarload":
                    return AccessLevel.HangarLoad;
                case "hangarloadother":
                    return AccessLevel.HangarLoadOther;
                case "bankwithdraw":
                    return AccessLevel.BankWithdraw;
                case "shipyardstart":
                    return AccessLevel.ShipyardStart;
                case "shipyardclaim":
                    return AccessLevel.ShipyardClaim;
                case "shipyardclaimother":
                    return AccessLevel.ShipyardClaimOther;
                case "dividendpay":
                    return AccessLevel.DividendPay;
                case "invite":
                    return AccessLevel.Invite;
                case "kick":
                    return AccessLevel.Kick;
                case "revokelowertitle":
                    return AccessLevel.RevokeLowerTitle;
                case "grantlowertitle":
                    return AccessLevel.GrantLowerTitle;
                case "removeenemy":
                    return AccessLevel.RemoveEnemy;
                case "addenemy":
                    return AccessLevel.AddEnemy;
                case "payfrombank":
                    return AccessLevel.PayFromBank;
                case "recievedividend":
                    return AccessLevel.RecieveDividend;
                case "taxexempt":
                    return AccessLevel.TaxExempt;
                case "changetax":
                    return AccessLevel.ChangeTax;
                case "unabletoparse":
                    return AccessLevel.UnableToParse;
            }
            return AccessLevel.UnableToParse;
        }
        [Command("tax", "set a ranks tax rate")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceTax(string rank, string tax)
        {
            if (IsNotInAlliance(out var alliance)) return;
            float amount = float.Parse(tax.Replace("%", ""));
            amount = amount / 100;
            if (amount > 0.5f)
            {
                Context.Respond("Maximum tax rate is 50%");
                return;
            }
            if (amount < 0)
            {
                Context.Respond("Tax rate cannot be negative.");
                return;
            }
            if (alliance != null)
            {
                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.ChangeTax))
                {
                    if (rank.ToLower().Equals("leader"))
                    {
                        alliance.leadertax = amount;
                        Context.Respond("Updated tax rate for leader.");
                        AlliancePlugin.SaveAllianceData(alliance);
                    }
                    else
                    {
                        if (rank.ToLower().Equals("unranked"))
                        {
                            alliance.UnrankedPerms.taxRate = amount;
                            Context.Respond("Updated tax rate for unranked.");
                            AlliancePlugin.SaveAllianceData(alliance);
                        }
                        else
                        {
                            if (alliance.CustomRankPermissions.ContainsKey(rank))
                            {
                                alliance.CustomRankPermissions[rank].taxRate = amount;
                                Context.Respond("Updated tax rate for " + rank + ".");
                                AlliancePlugin.SaveAllianceData(alliance);
                            }
                            else
                            {
                                Context.Respond("That rank doesnt exist.");
                            }
                        }
                    }
                }

                else
                {
                    Context.Respond("You dont have permission.");
                }
            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }
        [Command("rank permissions", "set a ranks permissions")]
        [Permission(MyPromoteLevel.None)]
        public void AlliancePermissions(string rank, string permission, Boolean enabled)
        {
            if (IsNotInAlliance(out var alliance)) return;
            if (!Enum.TryParse(permission, out AccessLevel level))
            {
                Context.Respond("Unable to read that permission, you can change, HangarSave, HangarLoad, HangarLoadOther, Kick, Invite, ShipyardStart, ShipyardClaim, ShipyardClaimOther, DividendPay, BankWithdraw, PayFromBank, AddEnemy, RemoveEnemy, GrantLowerTitle, Vote, RecieveDividend, TaxExempt, ChangeTax, RevokeLowerTitle.");
                return;
            }

            if (alliance != null)
            {
                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {
                    if (rank.ToLower().Equals("unranked"))
                    {
                        if (enabled)
                        {
                            if (!alliance.UnrankedPerms.permissions.Contains(level))
                                alliance.UnrankedPerms.permissions.Add(level);
                        }
                        else
                        {
                            if (alliance.UnrankedPerms.permissions.Contains(level))
                                alliance.UnrankedPerms.permissions.Remove(level);
                        }
                    }
                    else
                    {
                        if (enabled)
                        {
                            if (!alliance.CustomRankPermissions[rank].permissions.Contains(level))
                                alliance.CustomRankPermissions[rank].permissions.Add(level);
                        }
                        else
                        {
                            if (alliance.CustomRankPermissions[rank].permissions.Contains(level))
                                alliance.CustomRankPermissions[rank].permissions.Remove(level);
                        }
                    }

                    Context.Respond("Updated that permission level for unrankeds.");


                    AlliancePlugin.SaveAllianceData(alliance);
                }
                else
                {
                    Context.Respond("You dont have permission or that rank doesnt exist.");
                }
            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }
        [Command("view permissions", "view the permissions")]
        [Permission(MyPromoteLevel.None)]
        public void ViewPermissions()
        {
            if (IsNotInAlliance(out var alliance)) return;

            if (alliance != null)
            {
                StringBuilder sb = new StringBuilder();
                StringBuilder perms = new StringBuilder();
                foreach (String key in alliance.CustomRankPermissions.Keys)
                {
                    foreach (AccessLevel level in alliance.CustomRankPermissions[key].permissions)
                    {

                        perms.Append(level.ToString() + ", ");
                    }
                    sb.AppendLine(key + " Permissions : " + perms.ToString());
                    sb.AppendLine(key + " Permission Level " + alliance.CustomRankPermissions[key].permissionLevel);
                }
                perms.Clear();
                sb.AppendLine("");
                foreach (AccessLevel level in alliance.UnrankedPerms.permissions)
                {
                    perms.Append(level.ToString() + ", ");
                }
                sb.AppendLine("unranked Permissions : " + perms.ToString());
                sb.AppendLine("");
                foreach (KeyValuePair<ulong, RankPermissions> player in alliance.playerPermissions)
                {
                    perms.Clear();
                    foreach (AccessLevel level in player.Value.permissions)
                    {
                        perms.Append(level.ToString() + ", ");
                    }
                    sb.AppendLine(AlliancePlugin.GetPlayerName(player.Key) + " " + perms.ToString());
                }

                DialogMessage m = new DialogMessage("Alliance Permissions", alliance.name, sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond("You arent a member of an alliance.");
            }
        }
        [Command("level", "edit the permission level")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceCreateRank(string rankName, int permissionLevel)
        {
            if (IsNotInAlliance(out var alliance)) return;


            if (alliance != null)
            {
                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {


                    if (alliance.CustomRankPermissions.ContainsKey(rankName))
                    {

                        RankPermissions bob;
                        bob = alliance.CustomRankPermissions[rankName];
                        bob.permissionLevel = permissionLevel;
                        alliance.CustomRankPermissions[rankName] = bob;
                        Context.Respond("Rank edited!");
                        AlliancePlugin.SaveAllianceData(alliance);
                    }
                    else
                    {
                        Context.Respond("That rank doesnt exist.");
                    }

                }
                else
                {
                    Context.Respond("You dont have permission to create ranks");
                }

            }


            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }


        [Command("make rank", "make a rank")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceMakeNewRank(string rankName, int permissionLevel = 100)
        {
            if (IsNotInAlliance(out var alliance)) return;


            if (alliance != null)
            {
                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {


                    if (alliance.CustomRankPermissions.ContainsKey(rankName))
                    {
                        Context.Respond("Rank with that name already exists!");
                    }
                    else
                    {
                        RankPermissions bob = new RankPermissions();
                        bob.permissionLevel = permissionLevel;
                        bob.taxRate = 0f;
                        alliance.CustomRankPermissions.Add(rankName, bob);
                        Context.Respond("Rank created!");
                        AlliancePlugin.SaveAllianceData(alliance);
                    }

                }
                else
                {
                    Context.Respond("You dont have permission to create ranks");
                }

            }


            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }

        [Command("delete rank", "create a rank")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceDeleteRank(string rankName)
        {
            if (IsNotInAlliance(out var alliance)) return;
            if (alliance != null)
            {
                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {


                    if (alliance.CustomRankPermissions.ContainsKey(rankName))
                    {
                        List<ulong> yeetThese = new List<ulong>();
                        foreach (ulong id in alliance.PlayersCustomRank.Keys)
                        {
                            if (alliance.PlayersCustomRank[id].Equals(rankName))
                            {
                                yeetThese.Add(id);
                            }
                        }
                        foreach (ulong id in yeetThese)
                        {
                            alliance.PlayersCustomRank.Remove(id);
                        }
                        alliance.CustomRankPermissions.Remove(rankName);
                        Context.Respond("Rank deleted.");
                        AlliancePlugin.SaveAllianceData(alliance);
                    }
                    else
                    {
                        Context.Respond("Rank with that name doesnt exist!");

                    }

                }
                else
                {
                    Context.Respond("You dont have permission to delete ranks");
                }

            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }

        [Command("toggleforce", "toggle the forcing of friendly relations")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceToggleFriendly()
        {
            if (IsNotInAlliance(out var alliance)) return;
            if (alliance == null)
            {
                Context.Respond("You are not a member of an alliance.");
                return;
            }

            if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.AddEnemy))
            {
                Context.Respond("Toggled forcing of friendlies to " + !alliance.ForceFriends);
                alliance.ForceFriends = !alliance.ForceFriends;
                AlliancePlugin.SaveAllianceData(alliance);
            }
            else
            {
                Context.Respond("You do not have permission. AddEnemy permission node is required.");
            }
        }
        [Command("player permissions", "set a players permissions")]
        [Permission(MyPromoteLevel.None)]
        public void AlliancePlayerPermissions(string playerName, string permission, Boolean enabled)
        {
            if (IsNotInAlliance(out var alliance)) return;
            if (!Enum.TryParse(permission, out AccessLevel level))
            {
                Context.Respond("Unable to read that permission, you can change, HangarSave, HangarLoad, HangarLoadOther, Kick, Invite, ShipyardStart, ShipyardClaim, ShipyardClaimOther, DividendPay, BankWithdraw, PayFromBank, AddEnemy, RemoveEnemy, GrantLowerTitle, Vote, RecieveDividend, TaxExempt, ChangeTax, RevokeLowerTitle.");
                return;
            }
            MyIdentity id = AlliancePlugin.TryGetIdentity(playerName);
            if (id == null)
            {
                Context.Respond("Could not find that player");
                return;
            }
            MyFaction playerFac = MySession.Static.Factions.GetPlayerFaction(id.IdentityId);
            if (playerFac == null)
            {
                Context.Respond("That target player has no faction.");
                return;
            }

            if (alliance != null)
            {
                if (!alliance.AllianceMembers.Contains(playerFac.FactionId))
                {
                    Context.Respond("That target player isnt a member of the alliance.");
                    return;
                }
                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {

                    if (enabled)
                    {
                        if (alliance.playerPermissions.ContainsKey(MySession.Static.Players.TryGetSteamId(id.IdentityId)))
                        {
                            alliance.playerPermissions[MySession.Static.Players.TryGetSteamId(id.IdentityId)].permissions.Add(level);
                        }
                        else
                        {
                            RankPermissions bob = new RankPermissions();
                            bob.permissions.Add(level);
                            alliance.playerPermissions.Add(MySession.Static.Players.TryGetSteamId(id.IdentityId), bob);

                        }

                    }
                    else
                    {
                        if (alliance.playerPermissions.ContainsKey(MySession.Static.Players.TryGetSteamId(id.IdentityId)))
                        {
                            alliance.playerPermissions[MySession.Static.Players.TryGetSteamId(id.IdentityId)].permissions.Remove(level);
                        }

                    }
                    Context.Respond("Updated that permission level for the player.");
                    AlliancePlugin.SaveAllianceData(alliance);
                }
                else
                {
                    Context.Respond("You dont have permission to set permissions.");
                }

            }


            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }
        [Command("forceadd", "invite a faction to alliance")]
        [Permission(MyPromoteLevel.Admin)]
        public void AllianceForceAdd(string tag)
        {
            if (IsNotInAlliance(out var alliance)) return;
            IMyFaction fac2 = MySession.Static.Factions.TryGetFactionByTag(tag);
            if (fac2 == null)
            {
                Context.Respond("Cant find that faction.");
                return;
            }

            if (alliance != null)
            {
                if (alliance.HasPermissionToInvite(Context.Player.SteamUserId))
                {
                    alliance.SendInvite(fac2.FactionId);
                    alliance.ForceAddMember(fac2.FactionId);
                    AlliancePlugin.SaveAllianceData(alliance);

                    Context.Respond($"Invite sent, they can join using !alliance join \"{alliance.name}\"");
                }
                else
                {
                    Context.Respond("You dont have permission to send invites.");
                }
            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }

        [Command("invite", "invite a faction to alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceInvite(string tag)
        {
            if (IsNotInAlliance(out var alliance)) return;
            IMyFaction fac2 = MySession.Static.Factions.TryGetFactionByTag(tag);
            if (fac2 == null)
            {
                Context.Respond("Cant find that faction.");
                return;
            }

            if (alliance != null)
            {
                if (alliance.HasPermissionToInvite(Context.Player.SteamUserId))
                {
                    alliance.SendInvite(fac2.FactionId);
                    AlliancePlugin.SaveAllianceData(alliance);
                    AllianceChat.SendChatMessage(alliance.AllianceId, "Alliance", fac2.Tag + " was invited to the alliance!", true, 0);
                    Context.Respond("Invite sent, they can join using !alliance join " + alliance.name);
                }
                else
                {
                    Context.Respond("You dont have permission to send invites.");
                }
            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }
        [Command("reload", "reload the config")]
        [Permission(MyPromoteLevel.Admin)]
        public void AllianceInfo()
        {
            AlliancePlugin.LoadConfig();
            Context.Respond("Reloaded");
            AlliancePlugin.warcore.config = AlliancePlugin.utils.ReadFromJsonFile<WarConfig>(AlliancePlugin.path + "//OptionalWar//WarConfig.json");
        }
        [Command("takepoints", "take points from an alliance")]
        [Permission(MyPromoteLevel.Admin)]
        public void AllianceTakePoints(string name, int amount)
        {
            Boolean console = false;
            Alliance alliance = null;

            alliance = AlliancePlugin.GetAlliance(name);
            if (alliance == null)
            {
                Context.Respond($"Could not find that alliance.");
                return;
            }

            if (alliance.CurrentMetaPoints >= amount)
            {
                alliance.CurrentMetaPoints -= amount;
                AlliancePlugin.SaveAllianceData(alliance);
                Context.Respond("Points taken, new balance " + alliance.CurrentMetaPoints);
            }
            else
            {
                Context.Respond("Alliance does not have enough points.");
            }
        }
        [Command("members", "output members")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceMembers()
        {
            Boolean console = false;
            if (Context.Player == null)
            {
                console = true;
            }
            Alliance alliance = null;

            if (MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId) != null)
            {
                alliance = AlliancePlugin.GetAllianceNoLoading(MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId) as MyFaction);


            }
            else
            {
                Context.Respond("You must be in an alliance to use alliance commands.");
            }


            if (alliance == null)
            {
                Context.Respond("You are not a member of an alliance.");
                return;
            }
            if (!console)
            {
                DialogMessage m = new DialogMessage("Alliance Info", alliance.name, alliance.OutputMembers());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond("Alliance Info" + " " + alliance.name + alliance.OutputAlliance());
            }
        }
        [Command("dude", "output info for dude")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceDude()
        {


            Alliance alliance = null;

            if (MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId) != null)
            {
                alliance = AlliancePlugin.GetAlliance(MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId) as MyFaction);


            }
            else
            {
                Context.Respond("Must be a member of a faction.");
                return;
            }

            if (alliance == null)
            {
                Context.Respond("Could not find that alliance.");
                return;
            }
            DialogMessage m = new DialogMessage("Alliance Info", alliance.name, JsonConvert.SerializeObject(alliance));
            ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
        }

        [Command("upkeep", "take upkeep moneys")]
        [Permission(MyPromoteLevel.Admin)]
        public void DoAllUpkeep(string target)
        {
            if (target.ToLower().Equals("all") || target.ToLower().Contains("all"))
            {
                DatabaseForBank.DoUpkeepForAll();
                Context.Respond("Doing upkeep for all alliances.");
            }
            else
            {
                Alliance alliance = AlliancePlugin.GetAlliance(target);
                if (alliance == null)
                {
                    Context.Respond("Could not find that alliance.");
                    return;
                }
                DatabaseForBank.DoUpkeepForOne(alliance);
                Context.Respond("Upkeep done, if they paid it, idk");
            }
        }

        [Command("takemoney", "output info about an alliance")]
        [Permission(MyPromoteLevel.Admin)]
        public void AllianceRemoveMoney(string name, string inputAmount)
        {

            Alliance alliance = null;

            alliance = AlliancePlugin.GetAlliance(name);

            if (alliance == null)
            {
                Context.Respond("Could not find that alliance.");
                return;
            }
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Must be a positive amount", Color.Red, "Bank Man");
                return;
            }
            alliance.AdminWithdraw(amount, 1);
            AlliancePlugin.SaveAllianceData(alliance);
            DatabaseForBank.RemoveFromBalance(alliance, amount);

        }
        [Command("addmoney", "output info about an alliance")]
        [Permission(MyPromoteLevel.Admin)]
        public void AllianceAddMoney(string name, string inputAmount)
        {

            Alliance alliance = null;

            alliance = AlliancePlugin.GetAllianceNoLoading(name);

            if (alliance == null)
            {
                Context.Respond("Could not find that alliance.");
                return;
            }
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Must be a positive amount", Color.Red, "Bank Man");
                return;
            }
            DatabaseForBank.AddToBalance(alliance, amount);
            alliance.AdminAdd(amount, 1);
            AlliancePlugin.SaveAllianceData(alliance);
        }
        [Command("info", "output info about an alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceInfo(string name = "")
        {
            Boolean console = false;
            if (Context.Player == null)
            {
                console = true;
            }
            Alliance alliance = null;
            if (name.Equals(""))
            {
                if (MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId) != null)
                {
                    alliance = AlliancePlugin.GetAlliance(MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId) as MyFaction);


                }
            }
            else
            {
                alliance = AlliancePlugin.GetAllianceNoLoading(name);
            }
            if (alliance == null)
            {
                Context.Respond("Could not find that alliance.");
                return;
            }
            if (!console)
            {
                DialogMessage m = new DialogMessage("Alliance Info", alliance.name, alliance.OutputAlliance());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond("Alliance Info" + " " + alliance.name + alliance.OutputAlliance());

            }
        }
        [Command("leave", "leave the alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceLeave(string tag)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            if (fac.IsFounder(Context.Player.IdentityId))
            {
                Alliance alliance = AlliancePlugin.GetAlliance(fac);
                if (alliance == null)
                {
                    Context.Respond("Could not find that alliance.");
                    return;
                }
                foreach (MyFactionMember m in fac.Members.Values)
                {
                    if (alliance.SupremeLeader.Equals(MySession.Static.Players.TryGetSteamId(m.PlayerId)))
                    {
                        Context.Respond("The " + alliance.LeaderTitle + " Cannot leave the alliance, Leadership must be transferred first.");
                        return;
                    }
                    if (alliance.PlayersCustomRank.ContainsKey(MySession.Static.Players.TryGetSteamId(m.PlayerId)))
                    {
                        alliance.PlayersCustomRank.Remove(MySession.Static.Players.TryGetSteamId(m.PlayerId));
                    }

                }
                AllianceChat.SendChatMessage(alliance.AllianceId, "Alliance", fac.Tag + " has left the alliance!", true, 0);
                alliance.AllianceMembers.Remove(fac.FactionId);
                alliance.EnemyFactions.Add(fac.FactionId);
                foreach (MyFactionMember m in fac.Members.Values)
                {
                    AllianceChat.PeopleInAllianceChat.Remove(MySession.Static.Players.TryGetSteamId(m.PlayerId));
                }
                AlliancePlugin.SaveAllianceData(alliance);

            }
            else
            {
                Context.Respond("Only a Founder can leave the alliance");
            }
        }
        [Command("kick", "kick a faction from the alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceKick(string tag)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            IMyFaction fac2 = MySession.Static.Factions.TryGetFactionByTag(tag);
            if (fac2 == null)
            {
                Context.Respond("Cant find that faction.");
                return;
            }

            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance != null)
            {
                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.Kick))
                {
                    if (alliance.AllianceMembers.Contains(fac2.FactionId))
                    {
                        bool CanKick = true;
                        foreach (MyFactionMember m in fac2.Members.Values)
                        {
                            if (alliance.SupremeLeader.Equals(MySession.Static.Players.TryGetSteamId(m.PlayerId)) || alliance.PlayersCustomRank.ContainsKey(MySession.Static.Players.TryGetSteamId(m.PlayerId)))
                            {
                                CanKick = false;
                            }
                        }
                        if (alliance.SupremeLeader == Context.Player.SteamUserId)
                        {
                            CanKick = true;
                        }
                        if (CanKick)
                        {
                            alliance.AllianceMembers.Remove(fac2.FactionId);
                            AlliancePlugin.SaveAllianceData(alliance);
                            AllianceChat.SendChatMessage(alliance.AllianceId, "Alliance", fac2.Tag + " was kicked from the alliance!", true, 0);
                            foreach (long id in alliance.AllianceMembers)
                            {

                                IMyFaction member = MySession.Static.Factions.TryGetFactionById(id);
                                if (member != null)
                                {
                                    MyFactionCollection.DeclareWar(member.FactionId, fac2.FactionId);
                                    MySession.Static.Factions.SetReputationBetweenFactions(id, fac2.FactionId, -1500);

                                    foreach (MyFactionMember m in member.Members.Values)
                                    {
                                        AllianceChat.PeopleInAllianceChat.Remove(MySession.Static.Players.TryGetSteamId(m.PlayerId));
                                    }
                                }
                            }
                        }
                        else
                        {
                            Context.Respond("Cannot kick that faction while their members hold a rank.");
                        }
                    }
                    else
                    {
                        Context.Respond("That faction isnt a member of the alliance.");
                    }
                }
                else
                {
                    Context.Respond("You dont have permission to kick members.");
                }
            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }

        [Command("peace", "remove an enemy of the alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AlliancePeace(string type, string tag)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }


            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance != null)
            {
                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.RemoveEnemy))
                {
                    IMyFaction fac2;
                    switch (type.ToLower())
                    {
                        case "faction":
                            fac2 = MySession.Static.Factions.TryGetFactionByTag(tag);
                            if (fac2 == null)
                            {
                                Context.Respond("Cant find that faction.");
                                return;
                            }
                            if (alliance.EnemyFactions.Contains(fac2.FactionId))
                            {
                                alliance.EnemyFactions.Remove(fac2.FactionId);
                                AlliancePlugin.SaveAllianceData(alliance);


                            }
                            Context.Respond("Removed from enemy list.");
                            break;
                        case "fac":
                            fac2 = MySession.Static.Factions.TryGetFactionByTag(tag);
                            if (fac2 == null)
                            {
                                Context.Respond("Cant find that faction.");
                                return;
                            }
                            if (alliance.EnemyFactions.Contains(fac2.FactionId))
                            {
                                alliance.EnemyFactions.Remove(fac2.FactionId);
                                AlliancePlugin.SaveAllianceData(alliance);


                            }
                            Context.Respond("Removed from enemy list.");
                            break;
                        case "other":
                            Alliance enemy = AlliancePlugin.GetAllianceNoLoading(tag.Replace("\"", ""));
                            if (enemy != null)
                            {
                                if (alliance.enemies.Contains(tag))
                                {
                                    alliance.enemies.Remove(tag);
                                    AlliancePlugin.SaveAllianceData(alliance);

                                    Context.Respond("Removed from enemy list.");
                                }
                            }
                            else
                            {
                                Context.Respond("Could not find that alliance.");
                            }
                            break;
                        default:
                            Context.Respond("Error, use faction, fac or other as type.");
                            break;
                    }
                }
                else
                {
                    Context.Respond("You dont have permission to declare enemies.");
                }
            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }
        [Command("enemy", "declare an enemy of the alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceEnemy(string type, string tag)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }


            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance != null)
            {
                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.AddEnemy))
                {
                    IMyFaction fac2;
                    switch (type.ToLower())
                    {
                        case "faction":
                            fac2 = MySession.Static.Factions.TryGetFactionByTag(tag);
                            if (fac2 == null)
                            {
                                Context.Respond("Cant find that faction.");
                                return;
                            }
                            if (!alliance.EnemyFactions.Contains(fac2.FactionId))
                            {
                                alliance.EnemyFactions.Add(fac2.FactionId);
                                AlliancePlugin.SaveAllianceData(alliance);

                                alliance.ForceEnemies();
                            }
                            Context.Respond("War declared");
                            break;
                        case "fac":
                            fac2 = MySession.Static.Factions.TryGetFactionByTag(tag);
                            if (fac2 == null)
                            {
                                Context.Respond("Cant find that faction.");
                                return;
                            }
                            if (!alliance.EnemyFactions.Contains(fac2.FactionId))
                            {
                                alliance.EnemyFactions.Add(fac2.FactionId);
                                AlliancePlugin.SaveAllianceData(alliance);

                                alliance.ForceEnemies();
                            }
                            Context.Respond("War declared");
                            break;
                        case "other":
                            Alliance enemy = AlliancePlugin.GetAllianceNoLoading(tag);
                            if (enemy != null)
                            {
                                if (!alliance.enemies.Contains(enemy.name))
                                {
                                    alliance.enemies.Add(tag);
                                    AlliancePlugin.SaveAllianceData(alliance);

                                    alliance.ForceEnemies();
                                    Context.Respond("War declared");
                                }
                                else
                                {
                                    Context.Respond("Already at war with that alliance.");
                                }

                            }
                            else
                            {
                                Context.Respond("Could not find that alliance.");

                            }
                            break;
                        default:
                            Context.Respond("Error, use faction, fac or other as type.");
                            return;


                    }
                }
                else
                {
                    Context.Respond("You dont have permission to declare enemies.");
                }
            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }

        [Command("uninvite", "revoke a factions invite to alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceRevoke(string tag)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            IMyFaction fac2 = MySession.Static.Factions.TryGetFactionByTag(tag);
            if (fac2 == null)
            {
                Context.Respond("Cant find that faction.");
                return;
            }

            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance != null)
            {
                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.Invite))
                {
                    alliance.RevokeInvite(fac2.FactionId);
                    AlliancePlugin.SaveAllianceData(alliance);

                    Context.Respond("Invite revoked.");
                }
                else
                {
                    Context.Respond("You dont have permission to revoke invites.");
                }
            }
            else
            {
                Context.Respond("Cannot find alliance, maybe wait a minute and try again.");
            }
        }
        [Command("set title", "change a title")]
        [Permission(MyPromoteLevel.None)]
        public void SetTitleName(string title, string newName)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Match match = Regex.Match(newName, "^[0-9a-zA-Z ]{3,25}$", RegexOptions.IgnoreCase);
            if (!match.Success || string.IsNullOrEmpty(newName))
            {
                Context.Respond("New Title does not validate, try again.");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance != null)
            {
                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {

                    if (title.ToLower().Equals("leader"))
                    {
                        alliance.LeaderTitle = newName;
                        AlliancePlugin.SaveAllianceData(alliance);
                        AlliancePlugin.SendChatMessage("AllianceTitleConfig", "[A] " + newName + " ", Context.Player.SteamUserId);
                        //  Context.Respond("Updated");
                        return;
                    }
                    else
                    {
                        if (alliance.CustomRankPermissions.ContainsKey(title) && !alliance.CustomRankPermissions.ContainsKey(newName))
                        {
                            RankPermissions temp = alliance.CustomRankPermissions[title];
                            alliance.CustomRankPermissions.Remove(title);
                            alliance.CustomRankPermissions.Add(newName, temp);
                            List<ulong> fuckfuck = new List<ulong>();
                            Context.Respond("Updated the title.");
                            foreach (KeyValuePair<ulong, string> fuck in alliance.PlayersCustomRank)
                            {
                                if (fuck.Value.Equals(title))
                                {
                                    fuckfuck.Add(fuck.Key);
                                    AlliancePlugin.SendChatMessage("AllianceTitleConfig", "[A] " + newName + " ", fuck.Key);
                                }
                            }
                            foreach (ulong id in fuckfuck)
                            {
                                alliance.PlayersCustomRank[id] = newName;
                            }
                            AlliancePlugin.SaveAllianceData(alliance);
                        }
                        else
                        {
                            Context.Respond("Could not find that title. Or changing it would conflict.");
                        }
                    }
                }
                else
                {
                    Context.Respond("Only the " + alliance.LeaderTitle + " can change titles.");
                }
            }
            else
            {
                Context.Respond("Cannot find your alliance.");
            }
        }
        public static Dictionary<long, DateTime> confirmations = new Dictionary<long, DateTime>();
        [Command("dividend", "pay dividends to members online within the last 10 days, or the optional input")]
        [Permission(MyPromoteLevel.None)]
        public void Dividend(string inputAmount, int cutoffDays = 10)
        {
            if (!DatabaseForBank.ReadyToSave)
            {
                Context.Respond("Bank is disabled while it cannot connect to database.");
                return;
            }
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }

            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Must be a positive amount", Color.Red, "Bank Man");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);


            var cutoff = DateTime.Now - TimeSpan.FromDays(cutoffDays);
            if (alliance != null)
            {

                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.DividendPay))
                {
                    alliance.bankBalance = DatabaseForBank.GetBalance(alliance.AllianceId);
                    long AmountPerMember;
                    if (alliance.bankBalance >= amount)
                    {
                        List<long> idsToPay = new List<long>();
                        foreach (long id in alliance.AllianceMembers)
                        {
                            IMyFaction faction = MySession.Static.Factions.TryGetFactionById(id);
                            if (faction != null)
                            {
                                foreach (KeyValuePair<long, MyFactionMember> mem in faction.Members)
                                {
                                    MyIdentity idenid = MySession.Static.Players.TryGetIdentity(mem.Value.PlayerId);
                                    DateTime referenceTime = idenid.LastLoginTime;
                                    if (idenid.LastLogoutTime > referenceTime)
                                        referenceTime = idenid.LastLogoutTime;
                                    if (referenceTime >= cutoff && alliance.HasAccess(MySession.Static.Players.TryGetSteamId(mem.Value.PlayerId), AccessLevel.RecieveDividend))
                                    {
                                        idsToPay.Add(mem.Value.PlayerId);

                                    }

                                }
                            }
                        }
                        if (DatabaseForBank.RemoveFromBalance(alliance, amount))
                        {
                            Context.Respond("Paying " + idsToPay.Count + " " + String.Format("{0:n0}", amount / idsToPay.Count) + " SC each.");
                            alliance.PayDividend(amount, idsToPay, Context.Player.SteamUserId);
                            AlliancePlugin.SaveAllianceData(alliance);

                        }
                        else
                        {
                            Context.Respond("Error on removing balance, is the database connected?");
                        }
                    }
                    else
                    {
                        Context.Respond("Alliance bank cannot afford that.");
                        return;
                    }
                }
                else
                {
                    Context.Respond("Only the " + alliance.LeaderTitle + " can pay dividends");
                }
            }
            else
            {
                Context.Respond("You are not a member of an alliance.");
            }

        }
        [Command("opentoall", "open the alliance to all")]
        [Permission(MyPromoteLevel.None)]
        public void OpenAll()
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }


            Alliance alliance = AlliancePlugin.GetAlliance(fac);



            if (alliance != null)
            {

                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {
                    alliance.IsOpenToAll = !alliance.IsOpenToAll;
                    Context.Respond("Changing the alliance open status to " + alliance.IsOpenToAll);
                    AlliancePlugin.SaveAllianceData(alliance);
                }
            }
        }

        [Command("disband", "disband the alliance")]
        [Permission(MyPromoteLevel.None)]
        public void Disband()
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }


            Alliance alliance = AlliancePlugin.GetAlliance(fac);



            if (alliance != null)
            {

                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {
                    if (confirmations.ContainsKey(Context.Player.IdentityId))
                    {
                        if (confirmations[Context.Player.IdentityId] >= DateTime.Now)
                        {
                            File.Delete(AlliancePlugin.path + "//AllianceData//" + alliance.AllianceId + ".json");
                            foreach (long id in alliance.AllianceMembers)
                            {
                                AlliancePlugin.FactionsInAlliances.Remove(id);
                            }
                            AlliancePlugin.AllAlliances.Remove(alliance.name);
                            Context.Respond("Alliance disbanded.");
                        }
                        else
                        {
                            Context.Respond("Time ran out, start again");
                            confirmations[Context.Player.IdentityId] = DateTime.Now.AddSeconds(20);
                        }
                    }
                    else
                    {
                        Context.Respond("Run command again within 20 seconds to confirm.");
                        confirmations.Add(Context.Player.IdentityId, DateTime.Now.AddSeconds(20));
                    }
                }
                else
                {
                    Context.Respond("Only the " + alliance.LeaderTitle + " can disband the alliance.");
                }
            }
        }
 
        [Command("name", "change the alliance name")]
        [Permission(MyPromoteLevel.None)]
        public void SetAllianceName(string name)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Regex regex = new Regex("^[0-9a-zA-Z ]{1,50}$");
            Match match = Regex.Match(name, "^[0-9a-zA-Z ]{1,50}$", RegexOptions.IgnoreCase);
            if (!match.Success || string.IsNullOrEmpty(name))
            {
                Context.Respond("New Name does not validate, try again.");
                return;
            }
            if (name.ToLower().Equals("all"))
            {
                Context.Respond("Cannot use that name.");
                return;

            }
            if (AlliancePlugin.AllAlliances.ContainsKey(name))
            {
                Context.Respond("Alliance with that name already exists.");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);

            if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
            {
                alliance.name = name;
                AlliancePlugin.SaveAllianceData(alliance);

                Context.Respond("Name updated");
                return;
            }
        }

        [Command("withdraw", "withdraw from the bank")]
        [Permission(MyPromoteLevel.None)]
        public void BankWithdraw(string inputAmount)
        {
            if (!DatabaseForBank.ReadyToSave)
            {
                Context.Respond("Bank is disabled while it cannot connect to database.");
                return;
            }
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Must be a positive amount", Color.Red, "Bank Man");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance == null)
            {
                Context.Respond("Only members of an alliance may access a bank.");
                return;
            }
            if (alliance != null)
            {
                alliance.bankBalance = DatabaseForBank.GetBalance(alliance.AllianceId);
                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.BankWithdraw))
                {
                    if (alliance.bankBalance >= amount)
                    {
                        if (DatabaseForBank.RemoveFromBalance(alliance, amount))
                        {
                            EconUtils.addMoney(Context.Player.IdentityId, amount);
                            alliance.WithdrawMoney(amount, Context.Player.SteamUserId);
                            AlliancePlugin.SaveAllianceData(alliance);
                            Context.Respond("Withdraw complete.");
                        }
                        else
                        {
                            Context.Respond("Error on removing the balance, is the database connected?");
                        }

                    }
                    else
                    {
                        Context.Respond("The alliance bank does not contain enough money.", Color.Red, "Bank Man");
                    }

                }
                else
                {
                    Context.Respond("You do not have access to the bank.");
                }
                return;

            }
        }

        [Command("sellrank", "count the log")]
        [Permission(MyPromoteLevel.None)]
        public void SellRank(string rankname, string action, string inputAmount = "100000000", bool dorankup = false)
        {
            Context.Respond("Valid actions are tax, deposited, shipyard fee, Example usage !alliance logsearch Crunch tax,deposited or !alliance logsearch 0 tax");
            String timeformat = "MM-dd-yyyy";
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (Context.Player != null)
            {

                //Do stuff with taking components from grid storage
                //GridCosts localGridCosts = GetComponentsAndCost(projectedGrid);
                //gridCosts.setComponents(localGridCosts.getComponents());
                IMyFaction faction = FacUtils.GetPlayersFaction(Context.Player.IdentityId);
                if (faction == null)
                {
                    Context.Respond("You must be in a faction to use alliance features.");
                    return;
                }
                Alliance alliance = AlliancePlugin.GetAlliance(faction as MyFaction);
                if (alliance == null)
                {
                    Context.Respond("You are not a member of an alliance with an unlocked shipyard.");
                    return;
                }
                if (!alliance.PlayersCustomRank.ContainsValue(rankname))
                {
                    Context.Respond("Cannot find that rank");
                    return;
                }
                StringBuilder sb = new StringBuilder();
                if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.GrantLowerTitle))
                {


                    BankLog log = alliance.GetLog();

                    ulong targetId = 0;
                    log.log.Reverse();
                    Dictionary<ulong, long> temporaryCounter = new Dictionary<ulong, long>();
                    List<ulong> promoted = new List<ulong>();
                    long totalPaid = 0;




                    if (!action.Contains(","))
                    {
                        foreach (BankLogItem item in log.log.Where(x => x.Action.Equals(action)))
                        {
                            if (temporaryCounter.TryGetValue(item.SteamId, out long current))
                            {
                                temporaryCounter[item.SteamId] += item.Amount;
                            }
                            else
                            {
                                temporaryCounter.Add(item.SteamId, item.Amount);
                            }
                        }

                    }
                    else
                    {
                        String[] actions = action.Split(',');
                        foreach (String s in actions)
                        {
                            String s2 = s.Replace(" ", "");
                            foreach (BankLogItem item in log.log.Where(x => x.Action.Equals(s2)))
                            {
                                if (temporaryCounter.TryGetValue(item.SteamId, out long current))
                                {
                                    temporaryCounter[item.SteamId] += item.Amount;
                                }
                                else
                                {
                                    temporaryCounter.Add(item.SteamId, item.Amount);
                                }
                            }
                        }
                    }



                    foreach (KeyValuePair<ulong, long> pair in temporaryCounter)
                    {
                        if (pair.Value >= amount)
                        {
                            if (alliance.PlayersCustomRank.TryGetValue(pair.Key, out string rank))
                            {
                                if (rank.Equals(rankname))
                                    continue;
                            }
                            if (dorankup)
                            {
                                if (!alliance.PlayersCustomRank.ContainsKey(pair.Key))
                                {
                                    alliance.PlayersCustomRank.Add(pair.Key, rankname);
                                    promoted.Add(pair.Key);
                                    sb.AppendLine("Promoted " + AlliancePlugin.GetPlayerName(pair.Key) + " to " + rankname);
                                }
                                else
                                {

                                    RankPermissions thisGuy = alliance.CustomRankPermissions[alliance.PlayersCustomRank[pair.Key]];
                                    RankPermissions newTitle = alliance.CustomRankPermissions[rankname];

                                    if (newTitle.permissionLevel > thisGuy.permissionLevel)
                                    {
                                        alliance.SetTitle(pair.Key, rankname);
                                        promoted.Add(pair.Key);
                                        sb.AppendLine("Promoted " + AlliancePlugin.GetPlayerName(pair.Key) + " to " + rankname);
                                    }


                                }

                            }
                            else
                            {
                                if (!alliance.PlayersCustomRank.ContainsKey(pair.Key))
                                {
                                    sb.AppendLine("Eligible " + AlliancePlugin.GetPlayerName(pair.Key) + " to " + rankname + " " + String.Format("{0:n0}", pair.Value));
                                }
                                else
                                {

                                    RankPermissions thisGuy = alliance.CustomRankPermissions[alliance.PlayersCustomRank[pair.Key]];
                                    RankPermissions newTitle = alliance.CustomRankPermissions[rankname];

                                    if (thisGuy.permissionLevel > newTitle.permissionLevel)
                                    {
                                        //this is weird code
                                    }
                                    else
                                    {
                                        sb.AppendLine("Eligible " + AlliancePlugin.GetPlayerName(pair.Key) + " to " + rankname + " " + String.Format("{0:n0}", pair.Value));
                                    }
                                }

                            }

                        }
                    }

                    AlliancePlugin.SaveAllianceData(alliance);

                    foreach (ulong id in promoted)
                    {
                        if (dorankup)
                        {
                            sb.AppendLine("Promoted " + AlliancePlugin.GetPlayerName(id) + " to " + rankname);
                        }
                        else
                        {

                        }
                    }

                    DialogMessage m = new DialogMessage("Alliance Promotions", alliance.name, sb.ToString());
                    ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
                }
                else
                {
                    Context.Respond("You dont have permission to do this. Required perm is GrantLowerTitle");
                }

                //output the people upgraded

            }


        }
        [Command("logsearch", "count the log")]
        [Permission(MyPromoteLevel.None)]
        public void BankLog(string targetnameOrSteamId, string action, string inputAmount = "1")
        {
            Context.Respond("Valid actions are tax, deposited, shipyard fee, Example usage !alliance logsearch Crunch tax,deposited or !alliance logsearch 0 tax");
            String timeformat = "MM-dd-yyyy";
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (Context.Player != null)
            {

                //Do stuff with taking components from grid storage
                //GridCosts localGridCosts = GetComponentsAndCost(projectedGrid);
                //gridCosts.setComponents(localGridCosts.getComponents());
                IMyFaction faction = FacUtils.GetPlayersFaction(Context.Player.IdentityId);
                if (faction == null)
                {
                    Context.Respond("You must be in a faction to use alliance features.");
                    return;
                }
                Alliance alliance = AlliancePlugin.GetAlliance(faction as MyFaction);
                if (alliance == null)
                {
                    Context.Respond("You are not a member of an alliance with an unlocked shipyard.");
                    return;
                }

                BankLog log = alliance.GetLog();
                StringBuilder sb = new StringBuilder();
                ulong targetId = 0;
                log.log.Reverse();
                Dictionary<ulong, long> temporaryCounter = new Dictionary<ulong, long>();
                long totalPaid = 0;
                if (targetnameOrSteamId.Equals("0") && amount > 0)
                {
                    if (!action.Contains(","))
                    {
                        foreach (BankLogItem item in log.log.Where(x => x.Action.Equals(action)))
                        {

                            if (temporaryCounter.TryGetValue(item.SteamId, out long amount2))
                            {
                                temporaryCounter[item.SteamId] += item.Amount;
                            }
                            else
                            {
                                temporaryCounter.Add(item.SteamId, item.Amount);
                            }
                        }
                    }
                    else
                    {
                        String[] actions = action.Split(',');
                        foreach (String s in actions)
                        {
                            String s2 = s.Replace(" ", "");
                            foreach (BankLogItem item in log.log.Where(x => x.Action.Equals(s2)))
                            {
                                if (temporaryCounter.TryGetValue(item.SteamId, out long amount2))
                                {
                                    temporaryCounter[item.SteamId] += item.Amount;
                                }
                                else
                                {
                                    temporaryCounter.Add(item.SteamId, item.Amount);
                                }
                            }
                        }
                    }

                    foreach (KeyValuePair<ulong, long> key in temporaryCounter)
                    {
                        if (key.Value > amount)
                        {
                            sb.AppendLine(AlliancePlugin.GetPlayerName(key.Key) + " total: " + String.Format("{0:n0}", key.Value));
                        }

                    }

                    DialogMessage m2 = new DialogMessage("Alliance Bank Records", alliance.name, sb.ToString());
                    ModCommunication.SendMessageTo(m2, Context.Player.SteamUserId);
                    return;
                }
                else
                {


                    if (targetnameOrSteamId.Equals("0"))
                    {
                        if (!action.Contains(","))
                        {
                            foreach (BankLogItem item in log.log.Where(x => x.Action.Equals(action)))
                            {
                                totalPaid += item.Amount;
                                if (item.FactionPaid > 0)
                                {
                                    IMyFaction fac = MySession.Static.Factions.TryGetFactionById(item.FactionPaid);
                                    if (fac != null)
                                    {
                                        sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " " + fac.Tag + " " + String.Format("{0:n0}", item.Amount) + " : new balance " + String.Format("{0:n0}", item.BankAmount));
                                    }
                                    else
                                    {
                                        sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " a now dead faction " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                                    }
                                    continue;
                                }
                                if (item.PlayerPaid > 0)
                                {
                                    sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " " + AlliancePlugin.GetPlayerName(item.PlayerPaid) + " " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                                }
                                else
                                {

                                    sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                                }
                            }
                            sb.AppendLine("");
                            sb.AppendLine("Total found for " + targetnameOrSteamId + " " + String.Format("{0:n0}", totalPaid));
                        }
                        else
                        {
                            String[] actions = action.Split(',');
                            foreach (String s in actions)
                            {
                                String s2 = s.Replace(" ", "");
                                foreach (BankLogItem item in log.log.Where(x => x.Action.Equals(s2)))
                                {
                                    totalPaid += item.Amount;
                                    if (item.FactionPaid > 0)
                                    {
                                        IMyFaction fac = MySession.Static.Factions.TryGetFactionById(item.FactionPaid);
                                        if (fac != null)
                                        {
                                            sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " " + fac.Tag + " " + String.Format("{0:n0}", item.Amount) + " : new balance " + String.Format("{0:n0}", item.BankAmount));
                                        }
                                        else
                                        {
                                            sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " a now dead faction " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                                        }
                                        continue;
                                    }
                                    if (item.PlayerPaid > 0)
                                    {
                                        sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " " + AlliancePlugin.GetPlayerName(item.PlayerPaid) + " " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                                    }
                                    else
                                    {

                                        sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                                    }
                                }
                            }
                        }
                        sb.AppendLine("");
                        sb.AppendLine("Total found for " + targetnameOrSteamId + " " + String.Format("{0:n0}", totalPaid));
                    }
                    else
                    {
                        MyIdentity identity = AlliancePlugin.GetIdentityByNameOrId(targetnameOrSteamId);
                        if (identity != null)
                        {
                            targetId = MySession.Static.Players.TryGetSteamId(identity.IdentityId);

                        }
                        else
                        {
                            try
                            {
                                targetId = ulong.Parse(targetnameOrSteamId);
                            }
                            catch (Exception ex)
                            {
                                Context.Respond("Error parsing that id, target name didnt find id, input wasnt steam id");
                                return;
                            }
                        }
                        if (!action.Contains(","))
                        {
                            foreach (BankLogItem item in log.log.Where(x => x.SteamId == targetId && x.Action.Equals(action)))
                            {
                                totalPaid += item.Amount;
                                sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + action + " " + String.Format("{0:n0}", item.Amount));
                            }
                        }
                        else
                        {
                            String[] actions = action.Split(',');
                            foreach (String s in actions)
                            {
                                String s2 = s.Replace(" ", "");
                                foreach (BankLogItem item in log.log.Where(x => x.SteamId == targetId && x.Action.Equals(s2)))
                                {
                                    totalPaid += item.Amount;
                                    sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + s2 + " " + String.Format("{0:n0}", item.Amount));
                                }
                            }
                        }
                    }
                    sb.AppendLine("");
                    sb.AppendLine("Total found for " + targetnameOrSteamId + " " + String.Format("{0:n0}", totalPaid));
                }

                DialogMessage m = new DialogMessage("Alliance Bank Records", alliance.name, sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }

        }

        [Command("adminlog", "View the bank log")]
        [Permission(MyPromoteLevel.Admin)]
        public void BankLog(string allianceName, string timeformat = "MM-dd-yyyy")
        {

            //Do stuff with taking components from grid storage
            //GridCosts localGridCosts = GetComponentsAndCost(projectedGrid);
            //gridCosts.setComponents(localGridCosts.getComponents());
            Alliance alliance = AlliancePlugin.GetAlliance(allianceName);
            if (alliance == null)
            {
                Context.Respond("Alliance not found");
                return;
            }

            BankLog log = alliance.GetLog();
            StringBuilder sb = new StringBuilder();
            log.log.Reverse();
            int i = 0;
            foreach (BankLogItem item in log.log)
            {
                i++;
                if (item.FactionPaid > 0)
                {
                    IMyFaction fac = MySession.Static.Factions.TryGetFactionById(item.FactionPaid);
                    if (fac != null)
                    {
                        sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " " + fac.Tag + " " + String.Format("{0:n0}", item.Amount) + " : new balance " + String.Format("{0:n0}", item.BankAmount));
                    }
                    else
                    {
                        sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " a now dead faction " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                    }
                    continue;
                }
                if (item.PlayerPaid > 0)
                {
                    sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " " + AlliancePlugin.GetPlayerName(item.PlayerPaid) + " " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                }
                else
                {

                    sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                }
            }
            File.WriteAllText($"{AlliancePlugin.path}/{allianceName}-{DateTime.Today.ToString(timeformat)}.txt", sb.ToString());

            Context.Respond("Done");

        }

        [Command("log", "View the bank log")]
        [Permission(MyPromoteLevel.None)]
        public void BankLog(string timeformat = "MM-dd-yyyy", bool ignoreKoth = false, int max = 100)
        {

            if (Context.Player != null)
            {

                //Do stuff with taking components from grid storage
                //GridCosts localGridCosts = GetComponentsAndCost(projectedGrid);
                //gridCosts.setComponents(localGridCosts.getComponents());
                IMyFaction faction = FacUtils.GetPlayersFaction(Context.Player.IdentityId);
                if (faction == null)
                {
                    Context.Respond("You must be in a faction to use alliance features.");
                    return;
                }
                Alliance alliance = AlliancePlugin.GetAlliance(faction as MyFaction);
                if (alliance == null)
                {
                    Context.Respond("You are not a member of an alliance with an unlocked shipyard.");
                    return;
                }

                BankLog log = alliance.GetLog();
                StringBuilder sb = new StringBuilder();
                log.log.Reverse();
                int i = 0;
                foreach (BankLogItem item in log.log)
                {
                    if (i > max)
                    {
                        break;
                    }
                    if (item.SteamId == 1 && ignoreKoth)
                    {
                        continue;
                    }
                    i++;
                    if (item.FactionPaid > 0)
                    {
                        IMyFaction fac = MySession.Static.Factions.TryGetFactionById(item.FactionPaid);
                        if (fac != null)
                        {
                            sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " " + fac.Tag + " " + String.Format("{0:n0}", item.Amount) + " : new balance " + String.Format("{0:n0}", item.BankAmount));
                        }
                        else
                        {
                            sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " a now dead faction " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                        }
                        continue;
                    }
                    if (item.PlayerPaid > 0)
                    {
                        sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " " + AlliancePlugin.GetPlayerName(item.PlayerPaid) + " " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                    }
                    else
                    {

                        sb.AppendLine(item.TimeClaimed.ToString(timeformat) + " : " + AlliancePlugin.GetPlayerName(item.SteamId) + " " + item.Action + " " + String.Format("{0:n0}", item.Amount) + " : new balance  " + String.Format("{0:n0}", item.BankAmount));
                    }
                }
                DialogMessage m = new DialogMessage("Alliance Bank Records", alliance.name, sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }

        }

        [Command("deposit", "deposit to the bank")]
        [Permission(MyPromoteLevel.None)]
        public void BankDeposit(string inputAmount)
        {
            if (!DatabaseForBank.ReadyToSave)
            {
                Context.Respond("Bank is disabled while it cannot connect to database.");
                return;
            }
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Must be a positive amount", Color.Red, "Bank Man");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance == null)
            {
                Context.Respond("Only members of an alliance may access a bank.");
                return;
            }
            if (alliance != null)
            {
                alliance.bankBalance = DatabaseForBank.GetBalance(alliance.AllianceId);
                if (EconUtils.getBalance(Context.Player.IdentityId) >= amount)
                {
                    if (DatabaseForBank.AddToBalance(alliance, amount))
                    {
                        EconUtils.takeMoney(Context.Player.IdentityId, amount);
                        alliance.DepositMoney(amount, Context.Player.SteamUserId);
                        AlliancePlugin.SaveAllianceData(alliance);
                    }
                    else
                    {
                        Context.Respond("Error on adding to balance, is the database connected?");
                    }
                }
                else
                {
                    Context.Respond("The alliance bank does not contain enough money.", Color.Red, "Bank Man");
                }

            }

            return;

        }

        public bool DoAlliancePay(string type, string nameortag, Int64 amount, Alliance alliance, ulong steamid)
        {
            if (type.ToLower().Equals("player"))
            {
                MyIdentity id = AlliancePlugin.TryGetIdentity(nameortag);
                if (id == null)
                {
                    Context.Respond("Could not find that player");
                    return false;
                }

                if (DatabaseForBank.RemoveFromBalance(alliance, amount))
                {
                    EconUtils.addMoney(id.IdentityId, amount);
                    alliance.PayPlayer(amount, steamid, MySession.Static.Players.TryGetSteamId(id.IdentityId));
                    return true;
                }
            }
            else
            {
                MyFaction playerFac = MySession.Static.Factions.TryGetFactionByTag(nameortag);

                if (playerFac == null)
                {
                    Context.Respond("That target player has no faction.");
                    return false;
                }

                if (DatabaseForBank.RemoveFromBalance(alliance, amount))
                {
                    EconUtils.addMoney(playerFac.FactionId, amount);
                    alliance.PayFaction(amount, steamid, playerFac.FactionId);
                    return true;
                }
            }

            return false;
        }

        [Command("pay", "pay a player from the bank")]
        [Permission(MyPromoteLevel.None)]
        public void GiveTitleName(string type, string nameortag, string inputAmount)
        {
            if (!DatabaseForBank.ReadyToSave)
            {
                Context.Respond("Bank is disabled while it cannot connect to database.");
                return;
            }
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Must be a positive amount", Color.Red, "Bank Man");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            if (alliance == null)
            {
                Context.Respond("You must be in an alliance to use the bank.");
                return;
            }

            if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.PayFromBank))
            {
                alliance.bankBalance = DatabaseForBank.GetBalance(alliance.AllianceId);
                if (alliance.bankBalance >= amount)
                {
                    var result = DoAlliancePay(type, nameortag, amount, alliance, Context.Player.SteamUserId);
                    if (result)
                    {
                        AlliancePlugin.SaveAllianceData(alliance);
                    }
                    else
                    {
                        Context.Respond("Payment failed");
                    }

                }
                else
                {
                    Context.Respond("The alliance bank does not contain enough money.", Color.Red, "Bank Man");
                }
            }
            else
            {
                Context.Respond("You do not have access to the bank.");
            }

        }

        [Command("oof", "crunch command")]
        [Permission(MyPromoteLevel.None)]
        public void DoDebug2()
        {
            if (Context.Player.SteamUserId == 76561198045390854)
            {
                MyAPIGateway.Utilities.ShowMessage("Crunch Testing", "Can you see this?");
            }
            else
            {
                Context.Respond("You no Crunch, no debug for you");
            }

        }
        FileUtils utils = new FileUtils();
        [Command("chat", "toggle alliance chat")]
        [Permission(MyPromoteLevel.None)]
        public void DoAllianceChat(string message = "")
        {
            AllianceChat.AddOrRemoveToChat(Context.Player.SteamUserId);
        }

        public static void SendStatusToClient(Boolean status, ulong steamId)
        {

            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            var chatStatus = new BoolStatus()
            {
                Enabled = status
            };
            var statusM = MyAPIGateway.Utilities.SerializeToBinary(chatStatus);
            var message = new ModMessage()
            {
                Type = "Chat",
                Member = statusM
            };

            var bytes = MyAPIGateway.Utilities.SerializeToBinary(message);

            MyAPIGateway.Multiplayer.SendMessageTo(8544, bytes, steamId);

        }
        public static void SendStatusToClient(ulong steamId)
        {

            if (!MyAPIGateway.Multiplayer.IsServer)
                return;
            var status = false || AllianceChat.PeopleInAllianceChat.ContainsKey(steamId);
            var chatStatus = new BoolStatus()
            {
                Enabled = status
            };
            var statusM = MyAPIGateway.Utilities.SerializeToBinary(chatStatus);
            var message = new ModMessage()
            {
                Type = "Chat",
                Member = statusM
            };

            var bytes = MyAPIGateway.Utilities.SerializeToBinary(message);

            MyAPIGateway.Multiplayer.SendMessageTo(8544, bytes, steamId);

        }
        [Command("grant title", "change a title")]
        [Permission(MyPromoteLevel.None)]
        public void GiveTitleName(string playerName, string Title)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Regex regex = new Regex("^[0-9a-zA-Z ]{3,25}$");
            Match match = Regex.Match(Title, "^[0-9a-zA-Z ]{3,25}$", RegexOptions.IgnoreCase);
            if (!match.Success || string.IsNullOrEmpty(Title))
            {
                Context.Respond("New Title does not validate, try again.");
                return;
            }
            MyIdentity id = AlliancePlugin.TryGetIdentity(playerName);
            if (id == null)
            {
                Context.Respond("Could not find that player");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            MyFaction playerFac = MySession.Static.Factions.GetPlayerFaction(id.IdentityId);
            if (playerFac == null)
            {
                Context.Respond("That target player has no faction.");
                return;
            }
            if (!alliance.AllianceMembers.Contains(playerFac.FactionId))
            {
                Context.Respond("That target player isnt a member of the alliance.");
                return;
            }
            if (alliance != null)
            {

                if (alliance.CustomRankPermissions.ContainsKey(Title))
                {


                    if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                    {
                        if (!alliance.PlayersCustomRank.ContainsKey(MySession.Static.Players.TryGetSteamId(id.IdentityId)))
                        {
                            alliance.PlayersCustomRank.Add(MySession.Static.Players.TryGetSteamId(id.IdentityId), Title);
                        }
                        else
                        {
                            alliance.PlayersCustomRank[MySession.Static.Players.TryGetSteamId(id.IdentityId)] = Title;
                        }

                        AlliancePlugin.SendChatMessage("AllianceTitleConfig", "[A] " + Title, MySession.Static.Players.TryGetSteamId(id.IdentityId));
                        AlliancePlugin.SaveAllianceData(alliance);

                        Context.Respond("Updated");
                    }
                    else
                    {
                        if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.GrantLowerTitle))
                        {
                            RankPermissions thisGuy = alliance.CustomRankPermissions[alliance.PlayersCustomRank[Context.Player.SteamUserId]];
                            RankPermissions newTitle = alliance.CustomRankPermissions[Title];

                            if (thisGuy.permissionLevel > newTitle.permissionLevel)
                            {
                                alliance.SetTitle(MySession.Static.Players.TryGetSteamId(id.IdentityId), Title);
                                AlliancePlugin.SaveAllianceData(alliance);
                                Context.Respond("Title granted!");
                                AlliancePlugin.SendChatMessage("AllianceTitleConfig", "[A] " + Title, MySession.Static.Players.TryGetSteamId(id.IdentityId));
                            }
                            else
                            {
                                Context.Respond("That rank is higher or same rank as you.");
                            }


                        }
                        else
                        {
                            Context.Respond("No permission to grant titles.");
                        }
                    }
                    return;
                }
                else
                {
                    if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.GrantLowerTitle))
                    {
                        alliance.SetTitle(MySession.Static.Players.TryGetSteamId(id.IdentityId), Title);
                        AlliancePlugin.SaveAllianceData(alliance);
                        Context.Respond("Title granted!");
                        AlliancePlugin.SendChatMessage("AllianceTitleConfig", "[A] " + Title, MySession.Static.Players.TryGetSteamId(id.IdentityId));
                    }
                    else
                    {
                        Context.Respond("No permission to grant titles.");
                    }
                }
            }

        }


        [Command("change leader", "change the leader of the alliance")]
        [Permission(MyPromoteLevel.None)]
        public void Abdicate(string playerName)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }

            MyIdentity id = AlliancePlugin.TryGetIdentity(playerName);
            if (id == null)
            {
                Context.Respond("Could not find that player");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            MyFaction playerFac = MySession.Static.Factions.GetPlayerFaction(id.IdentityId);
            if (playerFac == null)
            {
                Context.Respond("That target player has no faction.");
                return;
            }
            if (!alliance.AllianceMembers.Contains(playerFac.FactionId))
            {
                Context.Respond("That target player isnt a member of the alliance.");
                return;
            }
            if (alliance != null)
            {


                if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                {
                    alliance.SupremeLeader = MySession.Static.Players.TryGetSteamId(id.IdentityId);
                    AlliancePlugin.SaveAllianceData(alliance);

                    Context.Respond("They are now the alliance leader.");
                }
                else
                {
                    Context.Respond("Only the " + alliance.LeaderTitle + " can change the leader.");
                }
                return;


            }
        }
        [Command("revoke title", "change a title")]
        [Permission(MyPromoteLevel.None)]
        public void RevokeTitleName(string playerName, string Title)
        {
            MyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            if (fac == null)
            {
                Context.Respond("Only factions can be in alliances.");
                return;
            }
            Regex regex = new Regex("^[0-9a-zA-Z ]{3,25}$");
            Match match = Regex.Match(Title, "^[0-9a-zA-Z ]{3,25}$", RegexOptions.IgnoreCase);
            if (!match.Success || string.IsNullOrEmpty(Title))
            {
                Context.Respond("New Title does not validate, try again.");
                return;
            }
            MyIdentity id = AlliancePlugin.TryGetIdentity(playerName);
            if (id == null)
            {
                Context.Respond("Could not find that player");
                return;
            }
            Alliance alliance = AlliancePlugin.GetAlliance(fac);
            MyFaction playerFac = MySession.Static.Factions.GetPlayerFaction(id.IdentityId);
            if (playerFac == null)
            {
                Context.Respond("That target player has no faction so fuck em booting them anyway.");

            }
            if (alliance != null)
            {
                if (alliance.CustomRankPermissions.ContainsKey(Title))
                {


                    if (alliance.SupremeLeader.Equals(Context.Player.SteamUserId))
                    {
                        if (alliance.PlayersCustomRank.ContainsKey(MySession.Static.Players.TryGetSteamId(id.IdentityId)))
                        {
                            alliance.PlayersCustomRank.Remove(MySession.Static.Players.TryGetSteamId(id.IdentityId));
                        }
                        AlliancePlugin.SaveAllianceData(alliance);
                        AlliancePlugin.SendChatMessage("AllianceTitleConfig", "[A] ", MySession.Static.Players.TryGetSteamId(id.IdentityId));
                        Context.Respond("Updated");
                    }
                    else
                    {
                        if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.RevokeLowerTitle))
                        {
                            RankPermissions thisGuy = alliance.CustomRankPermissions[alliance.PlayersCustomRank[Context.Player.SteamUserId]];
                            RankPermissions newTitle = alliance.CustomRankPermissions[Title];

                            if (thisGuy.permissionLevel > newTitle.permissionLevel)
                            {
                                alliance.RemoveTitle(MySession.Static.Players.TryGetSteamId(id.IdentityId), Title);
                                AlliancePlugin.SaveAllianceData(alliance);
                                AlliancePlugin.SendChatMessage("AllianceTitleConfig", "[A] ", MySession.Static.Players.TryGetSteamId(id.IdentityId));
                            }
                            else
                            {
                                Context.Respond("That rank is higher or same rank as you.");
                            }


                        }
                        else
                        {
                            Context.Respond("No permission to revoke titles.");
                        }
                    }
                    return;
                }
                else
                {
                    if (alliance.HasAccess(Context.Player.SteamUserId, AccessLevel.RevokeLowerTitle))
                    {

                        alliance.RemoveTitle(MySession.Static.Players.TryGetSteamId(id.IdentityId), Title);
                        AlliancePlugin.SaveAllianceData(alliance);
                        AlliancePlugin.SendChatMessage("AllianceTitleConfig", "[A] ", MySession.Static.Players.TryGetSteamId(id.IdentityId));
                        Context.Respond("Revoked that guy.");

                    }
                    else
                    {
                        Context.Respond("No permission to revoke titles.");
                    }
                }
            }
        }


        [Command("resetupgrades", "reset and refund upgrade levels for shipyard and hangar")]
        [Permission(MyPromoteLevel.Admin)]
        public void AllianceYeetUpgrades(string inputAmount, Boolean Reset = false)
        {
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing amount", Color.Red, "Bank Man");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Must be a positive amount", Color.Red, "Bank Man");
                return;
            }
            Dictionary<Guid, long> refunds = new Dictionary<Guid, long>();
            foreach (Alliance alliance in AlliancePlugin.AllAlliances.Values)
            {
                PrintQueue queue = alliance.LoadPrintQueue();
                long RefundAmount = 0;
                if (queue != null)
                {
                    for (int i = 1; i < queue.SlotsUpgrade; i++)
                    {
                        if (ShipyardCommands.slotUpgrades.ContainsKey(i))
                        {
                            RefundAmount += ShipyardCommands.slotUpgrades[i].MoneyRequired;
                            RefundAmount += ShipyardCommands.slotUpgrades[i].MetaPointsRequired * 5000000;
                            foreach (KeyValuePair<MyDefinitionId, int> items in ShipyardCommands.slotUpgrades[i].getItemsRequired())
                            {
                                RefundAmount += items.Value * amount;
                            }
                        }
                    }
                    for (int i = 1; i < queue.SpeedUpgrade; i++)
                    {
                        if (ShipyardCommands.speedUpgrades.ContainsKey(i))
                        {
                            RefundAmount += ShipyardCommands.speedUpgrades[i].MoneyRequired;
                            RefundAmount += ShipyardCommands.speedUpgrades[i].MetaPointsRequired * 5000000;
                            foreach (KeyValuePair<MyDefinitionId, int> items in ShipyardCommands.speedUpgrades[i].getItemsRequired())
                            {
                                RefundAmount += items.Value * amount;
                            }
                        }
                    }
                }
                HangarData hangar = alliance.LoadHangar();
                if (hangar != null)
                {
                    for (int i = 1; i < hangar.SlotUpgradeNum; i++)
                    {
                        if (HangarCommands.slotUpgrades.ContainsKey(i))
                        {
                            RefundAmount += HangarCommands.slotUpgrades[i].MoneyRequired;
                            RefundAmount += HangarCommands.slotUpgrades[i].MetaPointsRequired * 5000000;
                            foreach (KeyValuePair<MyDefinitionId, int> items in HangarCommands.slotUpgrades[i].getItemsRequired())
                            {
                                RefundAmount += items.Value * amount;
                            }
                        }
                    }
                }
                refunds.Add(alliance.AllianceId, RefundAmount);

                if (Reset)
                {
                    queue.SlotsUpgrade = 0;
                    queue.SpeedUpgrade = 0;
                    hangar.SlotUpgradeNum = 0;
                    Context.Respond("Reset complete. To refund money use !alliance addmoney " + alliance.name + " " + String.Format("{0:n0}", RefundAmount));
                    alliance.SavePrintQueue(queue);
                    hangar.SaveHangar(alliance);

                }
                else
                {
                    Context.Respond("With current settings refund will be " + alliance.name + " " + String.Format("{0:n0}", RefundAmount) + " SC. for " + alliance.name);
                }

            }


        }

        [Command("create", "create a new alliance")]
        [Permission(MyPromoteLevel.None)]
        public void AllianceCreate(string name)
        {
            IMyFaction fac = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            name = Context.RawArgs;
            Regex regex = new Regex("^[0-9a-zA-Z ]{1,50}$");
            Match match = Regex.Match(name, "^[0-9a-zA-Z ]{1,50}$", RegexOptions.IgnoreCase);
            if (!match.Success || string.IsNullOrEmpty(name))
            {
                Context.Respond("Name does not validate, try again.");
                return;
            }
            if (name.ToLower().Equals("all"))
            {
                Context.Respond("Cannot use that name.");
                return;

            }
            if (AlliancePlugin.AllAlliances.ContainsKey(name))
            {
                Context.Respond("Alliance with that name already exists.");
                return;
            }
            if (MyBankingSystem.GetBalance(Context.Player.IdentityId) >= AlliancePlugin.config.PriceNewAlliance)
            {


                if (fac != null)
                {
                    if (fac.IsFounder(Context.Player.IdentityId))
                    {
                        foreach (KeyValuePair<String, Alliance> alliance in AlliancePlugin.AllAlliances)
                        {
                            if (alliance.Value.AllianceMembers.Contains(fac.FactionId))
                            {
                                Context.Respond("You cannot create an alliance while being a member of an alliance.");
                                return;
                            }
                        }
                        Alliance newAlliance = new Alliance();
                        newAlliance.name = name;

                        newAlliance.SupremeLeader = Context.Player.SteamUserId;
                        newAlliance.ForceAddMember(fac.FactionId);
                        EconUtils.takeMoney(Context.Player.IdentityId, AlliancePlugin.config.PriceNewAlliance);
                        newAlliance.CustomRankPermissions.Add("Admiral", new RankPermissions());
                        foreach (MyFactionMember m in fac.Members.Values)
                        {
                            if (m.IsLeader)
                            {
                                ulong steamId = Sync.Players.TryGetSteamId(m.PlayerId);
                                if (steamId > 0)
                                {
                                    newAlliance.PlayersCustomRank.Add(steamId, "Admiral");
                                }
                            }
                        }
                        newAlliance.CustomRankPermissions["Admiral"].permissions.Add(AccessLevel.HangarSave);
                        newAlliance.CustomRankPermissions["Admiral"].permissions.Add(AccessLevel.HangarLoad);
                        newAlliance.CustomRankPermissions["Admiral"].permissions.Add(AccessLevel.ShipyardClaim);
                        newAlliance.CustomRankPermissions["Admiral"].permissions.Add(AccessLevel.ShipyardStart);
                        newAlliance.CustomRankPermissions["Admiral"].permissions.Add(AccessLevel.Invite);
                        newAlliance.CustomRankPermissions["Admiral"].permissions.Add(AccessLevel.Kick);
                        newAlliance.CustomRankPermissions["Admiral"].permissions.Add(AccessLevel.Vote);
                        AlliancePlugin.AllAlliances.Add(name, newAlliance);
                        AlliancePlugin.FactionsInAlliances.Add(fac.FactionId, newAlliance.name);
                        AlliancePlugin.SaveAllianceData(newAlliance);
                        DatabaseForBank.CreateAllianceBank(newAlliance);
                    }
                    else
                    {
                        Context.Respond("Only the founder may create an alliance.");
                        return;
                    }
                }
                else
                {
                    Context.Respond("You must be a member of a faction to create an alliance.");
                    return;
                }
            }
            else
            {
                Context.Respond("Cannot afford to create an alliance, it costs " + String.Format("{0:n0}", AlliancePlugin.config.PriceNewAlliance) + " SC.");
                return;
            }
        }
    }
}
