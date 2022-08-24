﻿using AlliancesPlugin.Shipyard;
using NLog;
using NLog.Config;
using NLog.Targets;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Torch.API;
using Torch.API.Managers;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace AlliancesPlugin.Alliances
{
    public static class AllianceChat
    {
        public static Logger log = LogManager.GetLogger("AllianceChat");
        public static void ApplyLogging()
        {

            var rules = LogManager.Configuration.LoggingRules;

            for (int i = rules.Count - 1; i >= 0; i--)
            {

                var rule = rules[i];

                if (rule.LoggerNamePattern == "AllianceChat")
                    rules.RemoveAt(i);
            }



            var logTarget = new FileTarget
            {
                FileName = "Logs/AllianceChat-" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + ".txt",
                Layout = "${var:logStamp} ${var:logContent}"
            };

            var logRule = new LoggingRule("AllianceChat", LogLevel.Debug, logTarget)
            {
                Final = true
            };

            rules.Insert(0, logRule);

            LogManager.Configuration.Reload();
        }

        public static MyGps ScanChat(string input, string desc = null)
        {

            int num = 0;
            bool flag = true;
            MatchCollection matchCollection = Regex.Matches(input, "GPS:([^:]{0,32}):([\\d\\.-]*):([\\d\\.-]*):([\\d\\.-]*):");

            Color color = new Color(117, 201, 241);
            foreach (Match match in matchCollection)
            {
                string str = match.Groups[1].Value;
                double x;
                double y;
                double z;
                try
                {
                    x = Math.Round(double.Parse(match.Groups[2].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    y = Math.Round(double.Parse(match.Groups[3].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    z = Math.Round(double.Parse(match.Groups[4].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    if (flag)
                        color = (Color)new ColorDefinitionRGBA(match.Groups[5].Value);
                }
                catch (SystemException ex)
                {
                    continue;
                }
                MyGps gps = new MyGps()
                {
                    Name = str,
                    Description = desc,
                    Coords = new Vector3D(x, y, z),
                    GPSColor = color,
                    ShowOnHud = false
                };
                gps.UpdateHash();

                return gps;
            }
            return null;
        }

        public static Dictionary<ulong, Guid> PeopleInAllianceChat = new Dictionary<ulong, Guid>();

        public static void ReceiveChatMessage(AllianceChatMessage message)
        {
            Alliance alliance = AlliancePlugin.GetAllianceNoLoading(message.AllianceId);
            List<ulong> OtherMembers = new List<ulong>();
            //strip out world names here
            //if (MyMultiplayer.Static.HostName.Contains("SENDS"))
            //{
            //    WorldName = MyMultiplayer.Static.HostName.Replace("SENDS", "");
            //}
            foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
            {
                if (player.Identity.IdentityId == message.SenderId)
                {
                    NotificationMessage message2 = new NotificationMessage("You are in alliance chat", 5000, "Green");
                    ModCommunication.SendMessageTo(message2, player.Id.SteamId);
                    continue;
                }
                var fac = MySession.Static.Factions.TryGetPlayerFaction(player.Identity.IdentityId) as MyFaction;
                if (fac == null) continue;
                if (alliance.AllianceMembers.Contains(fac.FactionId))
                {
                    OtherMembers.Add(player.Id.SteamId);
                }
            }

            foreach (var id in OtherMembers)
            {
                ShipyardCommands.SendMessage(message.SenderPrefix.Replace("**", ""), message.MessageText, new Color(alliance.r, alliance.g, alliance.b), (long)id);
                var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;

                if (ScanChat(message.MessageText, null) == null) continue;
                var gpsRef = ScanChat(message.MessageText, null);
                gpsRef.GPSColor = Color.Yellow;
                gpsRef.AlwaysVisible = true;
                gpsRef.ShowOnHud = true;

                var idenId = MySession.Static.Players.TryGetIdentityId(id);
                gpscol.SendAddGpsRequest(idenId, ref gpsRef);
            }
        }

        public static void SendChatMessage(Guid allianceId, string prefix, string message, bool toDiscord, long playerId)
        {
            prefix = prefix.Replace(":", "");
            Alliance alliance = AlliancePlugin.GetAllianceNoLoading(allianceId);
            message = message.Replace("@", "");

            log.Info(allianceId.ToString() + " : " + alliance.name + " : " + prefix + " " + message);
            var SendingMessage = new AllianceChatMessage
            {
                SenderPrefix = $"**{prefix}**",
                MessageText = message,
                AllianceId = allianceId,
                ChannelId = alliance.DiscordChannelId,
                BotToken = alliance.DiscordToken,
                SenderId = playerId,
                FromDiscord = false
            };

            var input = JsonConvert.SerializeObject(SendingMessage);
            var methodInput = new object[] { "AllianceMessage", input };
            AlliancePlugin.SendMessage?.Invoke(AlliancePlugin.MQ, methodInput);
        }

        public static Dictionary<ulong, long> IdentityIds = new Dictionary<ulong, long>();
        public static void DoChatMessage(TorchChatMessage msg, ref bool consumed)
        {
         
            if (msg.AuthorSteamId == null)
            {
                return;
            }
            if (msg.Channel == Sandbox.Game.Gui.ChatChannel.Private || msg.Channel == Sandbox.Game.Gui.ChatChannel.Faction)
            {
                return;
            }
            if (msg.Message.StartsWith("!"))
            {
                return;
            }

            if (PeopleInAllianceChat.ContainsKey((ulong)msg.AuthorSteamId))
            {

                MyIdentity identity;
            if (IdentityIds.ContainsKey((ulong)msg.AuthorSteamId)){
                    identity = MySession.Static.Players.TryGetIdentity(IdentityIds[(ulong)msg.AuthorSteamId]);
                } else{
                    identity = AlliancePlugin.GetIdentityByNameOrId(msg.AuthorSteamId.ToString());
                }
             
                if (identity == null)
                {
                    return;
                }
                MyFaction fac = MySession.Static.Factions.GetPlayerFaction(identity.IdentityId);
                if (fac == null)
                {
                    bool noFac = true;
                    if (AlliancePlugin.GetIdentityByNameOrId(msg.Author) != null) 
                    {
                        if (MySession.Static.Factions.GetPlayerFaction(AlliancePlugin.GetIdentityByNameOrId(msg.Author).IdentityId) != null)
                        {
                            noFac = false;
                            fac = MySession.Static.Factions.GetPlayerFaction(AlliancePlugin.GetIdentityByNameOrId(msg.Author).IdentityId);
                        }
                    }
                      
                    if (noFac)
                    {
                        PeopleInAllianceChat.Remove((ulong)msg.AuthorSteamId);
                        AllianceCommands.SendStatusToClient(false, (ulong) msg.AuthorSteamId);
                        AlliancePlugin.SendChatMessage("Failsafe", "Faction null");
                    }
                    return;
                }
                if (AlliancePlugin.GetAllianceNoLoading(fac) == null)
                {
                    PeopleInAllianceChat.Remove((ulong)msg.AuthorSteamId);
                    AllianceCommands.SendStatusToClient(false, (ulong)msg.AuthorSteamId);
                    AlliancePlugin.SendChatMessage("Failsafe", "Alliance null");
                    return;
                }
                consumed = true;
                Guid allianceId = PeopleInAllianceChat[(ulong)msg.AuthorSteamId];
                List<ulong> OtherMembers = new List<ulong>();

                Alliance alliance = AlliancePlugin.GetAllianceNoLoading(allianceId);
                // ShipyardCommands.SendMessage(msg.Author, "You are in alliance chat", Color.BlueViolet, (long)msg.AuthorSteamId);
                if (alliance.GetTitle((ulong)msg.AuthorSteamId).Equals("")){
                    SendChatMessage(allianceId, msg.Author, msg.Message, true, identity.IdentityId);
       
                }
                else
                {
                    SendChatMessage(allianceId, alliance.GetTitle((ulong)msg.AuthorSteamId) + " | " + msg.Author, msg.Message, true, identity.IdentityId);
                }
              
            }
            else
            {
                //  PeopleInAllianceChat.Remove((ulong)msg.AuthorSteamId);
            }


        }
       public static FileUtils utils = new FileUtils();
        public static void Login(IPlayer p)
        {
            if (p == null)
            {
                return;
            }
           
            MyIdentity id = AlliancePlugin.GetIdentityByNameOrId(p.SteamId.ToString());
            if (id == null)
            {
                return;
            }
            IMyFaction playerFac = MySession.Static.Factions.GetPlayerFaction(id.IdentityId);
            MyFaction arrr = MySession.Static.Factions.TryGetFactionByTag("arrr");
            if (arrr != null)
            {
                if (playerFac != null && !MySession.Static.Factions.AreFactionsEnemies(arrr.FactionId, FacUtils.GetPlayersFaction(id.IdentityId).FactionId))
                {
                    Sandbox.Game.Multiplayer.MyFactionCollection.DeclareWar(playerFac.FactionId, arrr.FactionId);
                }
            }
     
            MyFaction ACME = MySession.Static.Factions.TryGetFactionByTag("ACME");

            if (ACME != null)
            {
                MySession.Static.Factions.SetReputationBetweenPlayerAndFaction(id.IdentityId, ACME.FactionId, 0);
                MySession.Static.Factions.AddFactionPlayerReputation(id.IdentityId, ACME.FactionId, 0);
            }
            MyFaction GAIA = MySession.Static.Factions.TryGetFactionByTag("GAIA");

            if (GAIA != null)
            {
                MySession.Static.Factions.SetReputationBetweenPlayerAndFaction(id.IdentityId, GAIA.FactionId, 0);
                MySession.Static.Factions.AddFactionPlayerReputation(id.IdentityId, GAIA.FactionId, 0);
            }
            MyFaction wolf = MySession.Static.Factions.TryGetFactionByTag("WOLF");
            if (wolf != null)
            {
                if (playerFac != null && !MySession.Static.Factions.AreFactionsEnemies(wolf.FactionId, FacUtils.GetPlayersFaction(id.IdentityId).FactionId))
                {
                    Sandbox.Game.Multiplayer.MyFactionCollection.DeclareWar(playerFac.FactionId, wolf.FactionId);
                }
            }

          
                if (File.Exists(AlliancePlugin.path + "//PlayerData//" + p.SteamId + ".xml") && playerFac != null)
            {
                AlliancePlugin.UpdateThese[p.SteamId] = DateTime.Now.AddSeconds(60);

               PlayerData data = utils.ReadFromXmlFile<PlayerData>(AlliancePlugin.path + "//PlayerData//" + p.SteamId + ".xml");
                if (data.InAllianceChat)
                {
                    if (AlliancePlugin.GetAllianceNoLoading(playerFac as MyFaction) != null)
                    {
                        AlliancePlugin.statusUpdate.Remove(p.SteamId);
                        AlliancePlugin.statusUpdate.Add(p.SteamId, true);
       
                        PeopleInAllianceChat.Remove(p.SteamId);
                        PeopleInAllianceChat.Add(p.SteamId, AlliancePlugin.GetAllianceNoLoading(playerFac as MyFaction).AllianceId);
                        AlliancePlugin.otherAllianceShit.Remove(p.SteamId);
                        AlliancePlugin.otherAllianceShit.Add(p.SteamId, AlliancePlugin.GetAllianceNoLoading(playerFac as MyFaction).AllianceId);

                    }
                }
                else
                {
                    if (AlliancePlugin.GetAllianceNoLoading(playerFac as MyFaction) != null)
                    {
                        AlliancePlugin.otherAllianceShit.Remove(p.SteamId);
                        AlliancePlugin.otherAllianceShit.Add(p.SteamId, AlliancePlugin.GetAllianceNoLoading(playerFac as MyFaction).AllianceId);
                    }
                }

            }
        }

        public static void Logout(IPlayer p)
        {
            //if (p == null)
            //{
            //    return;
            //}

            //MyIdentity id = AlliancePlugin.GetIdentityByNameOrId(p.SteamId.ToString());
            //if (id == null)
            //{
            //    return;
            //}
            //if (MySession.Static.Factions.TryGetFactionById(id.IdentityId) != null && AlliancePlugin.FactionsInAlliances.ContainsKey(MySession.Static.Factions.TryGetFactionById(id.IdentityId).FactionId))
            //{
            //    Alliance alliance = AlliancePlugin.GetAllianceNoLoading(MySession.Static.Factions.GetPlayerFaction(id.IdentityId) as MyFaction);
            //    if (AlliancePlugin.playersInAlliances.ContainsKey(alliance.AllianceId))
            //    {
            //        if (AlliancePlugin.playersInAlliances[alliance.AllianceId].Contains(p.SteamId))
            //        {
            //            AlliancePlugin.playersInAlliances[alliance.AllianceId].Remove(p.SteamId);
            //        }
            //    }
            //}

        }
    }
}
