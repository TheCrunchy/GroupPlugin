using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Net.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using AlliancesPlugin.KOTH;
using AlliancesPlugin.Shipyard;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using System.IO;
using VRage.Game.ModAPI;

namespace AlliancesPlugin.Alliances
{
    public class DiscordStuff
    {

        //do the discord shit in here
        private static DiscordActivity game;
        private static ulong botId = 0;

        public static bool AllianceReady { get; set; } = false;
        public static bool Ready { get; set; } = false;
        public static DiscordClient Discord { get; set; }
        public static List<ulong> ChannelIds = new List<ulong>();
        public static Dictionary<Guid, DiscordClient> allianceBots = new Dictionary<Guid, DiscordClient>();
        private static Dictionary<ulong, Guid> allianceChannels = new Dictionary<ulong, Guid>();
        public static Task RegisterDiscord()
        {
            if (!Ready)
            {


                try
                {
                    // Windows Vista - 8.1
                    if (Environment.OSVersion.Platform.Equals(PlatformID.Win32NT) && Environment.OSVersion.Version.Major == 6)
                    {
                        Discord = new DiscordClient(new DiscordConfiguration
                        {
                            Token = AlliancePlugin.config.DiscordBotToken,
                            TokenType = TokenType.Bot,
                            WebSocketClientFactory = WebSocket4NetClient.CreateNew,
                            AutoReconnect = true
                        });
                    }
                    else
                    {
                        Discord = new DiscordClient(new DiscordConfiguration
                        {
                            Token = AlliancePlugin.config.DiscordBotToken,
                            TokenType = TokenType.Bot,
                            AutoReconnect = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    AlliancePlugin.Log.Error(ex);
                    Ready = false;
                    return Task.CompletedTask;
                }



                try
                {
                    Discord.ConnectAsync();
                }
                catch (Exception)
                {
                    return Task.CompletedTask;

                }
                Discord.MessageCreated += Discord_MessageCreated;
                game = new DiscordActivity();
                Ready = true;
            }
            return Task.CompletedTask;
        }
        public static List<Guid> registered = new List<Guid>();

        public static Task RegisterAllianceBot(Alliance alliance, ulong channelId)
        {
            if (!allianceBots.ContainsKey(alliance.AllianceId))
            {
                DiscordClient bot;

                try
                {

                    // Windows Vista - 8.1
                    if (Environment.OSVersion.Platform.Equals(PlatformID.Win32NT) && Environment.OSVersion.Version.Major == 6)
                    {
                        bot = new DiscordClient(new DiscordConfiguration
                        {
                            Token = Encryption.DecryptString(alliance.AllianceId.ToString(), alliance.DiscordToken),
                            TokenType = TokenType.Bot,
                            WebSocketClientFactory = WebSocket4NetClient.CreateNew,
                            AutoReconnect = true
                        });
                    }
                    else
                    {
                        bot = new DiscordClient(new DiscordConfiguration
                        {
                            Token = Encryption.DecryptString(alliance.AllianceId.ToString(), alliance.DiscordToken),
                            TokenType = TokenType.Bot,
                            AutoReconnect = true
                        });
                    }

                }
                catch (Exception ex)
                {
                    AlliancePlugin.Log.Error(ex);

                    return Task.CompletedTask;
                }


                try
                {
                    bot.ConnectAsync();
                    bot.MessageCreated += Discord_AllianceMessage;
                }
                catch (Exception ex)
                {
                    AlliancePlugin.Log.Error(ex);


                    return Task.CompletedTask;
                }
                if (!allianceBots.ContainsKey(alliance.AllianceId))
                {
            

                    allianceBots.Remove(alliance.AllianceId);
                    allianceBots.Add(alliance.AllianceId, bot);
                    allianceChannels.Remove(alliance.DiscordChannelId);

                    allianceChannels.Add(channelId, alliance.AllianceId);
                }
            }

            return Task.CompletedTask;
        }
        private static async void RunGameTask(Action obj)
        {
            if (AlliancePlugin.TorchBase.CurrentSession != null)
            {
                await AlliancePlugin.TorchBase.InvokeAsync(obj);
            }
            else
            {
                await Task.Run(obj);
            }
        }

        private static string WorldName = "";
        public static void DisconnectDiscord()
        {
            Ready = false;
            AllianceReady = false;
            foreach (DiscordClient bot in allianceBots.Values)
            {
                bot.DisconnectAsync();
            }
            Discord?.DisconnectAsync();
        }
        public static void SendMessageToDiscord(string message, KothConfig config)
        {
            if (Ready && AlliancePlugin.config.DiscordChannelId > 0 && config.doDiscordMessages)
            {
                DiscordChannel chann = Discord.GetChannelAsync(AlliancePlugin.config.DiscordChannelId).Result;
                botId = Discord.SendMessageAsync(chann, message.Replace("/n", "\n")).Result.Author.Id;


            }
            else
            {
                ShipyardCommands.SendMessage(config.KothName, message, Color.LightGreen, 0L);
            }
        }
        private static int attempt = 0;

        public static void SendAllianceMessage(Alliance alliance, string prefix, string message)
        {
            if (AllianceHasBot(alliance.AllianceId) && alliance.DiscordChannelId > 0)
            {

                DiscordClient bot = allianceBots[alliance.AllianceId];

                DiscordChannel chann = bot.GetChannelAsync(alliance.DiscordChannelId).Result;
                if (bot == null)
                {
                    return;
                }
                if (WorldName.Equals("") && MyMultiplayer.Static.HostName != null)
                {
                    if (MyMultiplayer.Static.HostName.Contains("SENDS"))
                    {

                        WorldName = MyMultiplayer.Static.HostName.Replace("SENDS", "");
                    }
                    else
                    {
                        if (MyMultiplayer.Static.HostName.Equals("Sigma Draconis Lobby"))
                        {
                            WorldName = "01";
                        }
                        else
                        {
                            WorldName = MyMultiplayer.Static.HostName;
                        }

                    }
                }
                try
                {
                    botId = bot.SendMessageAsync(chann, "**[" + WorldName + "] " + prefix + "**: " + message.Replace(" /n", "\n")).Result.Author.Id;
                    //bot.MessageCreated -= Discord_AllianceMessage;
                  //  bot.MessageCreated += Discord_AllianceMessage;
                   
                   
                }
                catch (DSharpPlus.Exceptions.RateLimitException)
                {
                    if (attempt <= 5)
                    {
                        attempt++;
                        SendAllianceMessage(alliance, prefix, message);
                        attempt = 0;
                    }
                    else
                    {
                        attempt = 0;
                    }
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    AllianceChat.SendChatMessageFromDiscord(alliance.AllianceId, "Bot", "Failed to send message.", 0);
                }
                catch (Exception ex)
                {
                    if (debugMode)
                    {
                        if (MySession.Static.Players.GetPlayerByName("Crunch") != null)
                        {
                            MyPlayer player = MySession.Static.Players.GetPlayerByName("Crunch");
                            ShipyardCommands.SendMessage("Discord", "" + ex.ToString(), Color.Blue, (long)player.Id.SteamId);
                        }
                    }
                }
            }
            else
            {
                if (debugMode)
                {
                    if (MySession.Static.Players.GetPlayerByName("Crunch") != null)
                    {
                        MyPlayer player = MySession.Static.Players.GetPlayerByName("Crunch");
                        ShipyardCommands.SendMessage("Discord", "doesnt have bot or channel id is 0", Color.Blue, (long)player.Id.SteamId);
                    }
                }
            }
        }
 

        public static string MakeLog(Alliance alliance, int max)
        {
            BankLog log = alliance.GetLog();
            StringBuilder sb = new StringBuilder();
            log.log.Reverse();
            int i = 0;
            sb.AppendLine("Time,Player,Action,Faction/Player,Amount,NewBalance");
            foreach (BankLogItem item in log.log)
            {
                i++;
                if (i >= max)
                {
                    return sb.ToString();
                }
                if (item.FactionPaid > 0)
                {
                    IMyFaction fac = MySession.Static.Factions.TryGetFactionById(item.FactionPaid);
                    if (fac != null)
                    {
                        sb.AppendLine(item.TimeClaimed.ToString() + "," + AlliancePlugin.GetPlayerName(item.SteamId) + "," + item.Action + "," + fac.Tag + " " + String.Format("{0:n0}", item.Amount) + "," + String.Format("{0:n0}", item.BankAmount));
                    }
                    else
                    {
                        sb.AppendLine(item.TimeClaimed.ToString() + "," + AlliancePlugin.GetPlayerName(item.SteamId) + "," + item.Action + " a now dead faction ," + String.Format("{0:n0}", item.Amount) + "," + String.Format("{0:n0}", item.BankAmount));
                    }
                    continue;
                }
                if (item.PlayerPaid > 0)
                {
                    sb.AppendLine(item.TimeClaimed.ToString() + "," + AlliancePlugin.GetPlayerName(item.SteamId) + "," + item.Action + "," + AlliancePlugin.GetPlayerName(item.PlayerPaid) + ", " + String.Format("{0:n0}", item.Amount) + "," + String.Format("{0:n0}", item.BankAmount));
                }
                else
                {

                    sb.AppendLine(item.TimeClaimed.ToString() + "," + AlliancePlugin.GetPlayerName(item.SteamId) + "," + item.Action + "," + String.Format("{0:n0}", item.Amount) + ", " + String.Format("{0:n0}", item.BankAmount));
                }
            }
            return sb.ToString();
        }
        public static async void SendAllianceLog(Alliance alliance, int max)
        {
            if (AllianceHasBot(alliance.AllianceId) && alliance.DiscordChannelId > 0)
            {

                DiscordClient bot = allianceBots[alliance.AllianceId];

                DiscordChannel chann = bot.GetChannelAsync(alliance.DiscordChannelId).Result;
                if (bot == null)
                {
                    return;
                }


               String output = await Task.Run(() => MakeLog(alliance, max));



                File.WriteAllText(AlliancePlugin.path + "//temp.txt", output);
                FileStream stream = new FileStream(AlliancePlugin.path + "//temp.txt", FileMode.Open);

                chann.SendFileAsync("LOG.txt", stream);


            }
        }
    
    public static bool AllianceHasBot(Guid id)
    {
        if (allianceBots.ContainsKey(id))
            return true;
        return false;
    }

    public static string GetStringBetweenCharacters(string input, char charFrom, char charTo)
    {
        int posFrom = input.IndexOf(charFrom);
        if (posFrom != -1) //if found char
        {
            int posTo = input.IndexOf(charTo, posFrom + 1);
            if (posTo != -1) //if found char
            {
                return input.Substring(posFrom + 1, posTo - posFrom - 1);
            }
        }

        return string.Empty;
    }
    public static Dictionary<ulong, string> nickNames = new Dictionary<ulong, string>();
    public static Boolean debugMode = false;
    public static Dictionary<Guid, string> LastMessageSent = new Dictionary<Guid, string>();
    private static Task Discord_AllianceMessage(DSharpPlus.EventArgs.MessageCreateEventArgs e)
    {
        if (debugMode)
        {
            if (MySession.Static.Players.GetPlayerByName("Crunch") != null)
            {
                MyPlayer player = MySession.Static.Players.GetPlayerByName("Crunch");
                ShipyardCommands.SendMessage("Discord", "Got a message " + e.Message.ChannelId + " " + e.Channel.Id, Color.Blue, (long)player.Id.SteamId);
            }
        }
        if (allianceChannels.ContainsKey(e.Channel.Id))
        {
            if (debugMode)
            {
                if (MySession.Static.Players.GetPlayerByName("Crunch") != null)
                {
                    MyPlayer player = MySession.Static.Players.GetPlayerByName("Crunch");
                    ShipyardCommands.SendMessage("Discord 1", "Is an alliance channel " + e.Message.ChannelId + " " + e.Channel.Id, Color.Blue, (long)player.Id.SteamId);
                }
            }
            if (e.Author.IsBot)
            {
                if (debugMode)
                {
                    if (MySession.Static.Players.GetPlayerByName("Crunch") != null)
                    {
                        MyPlayer player = MySession.Static.Players.GetPlayerByName("Crunch");
                        ShipyardCommands.SendMessage("Discord 2", "Bot message " + e.Message.ChannelId + " " + e.Channel.Id, Color.Blue, (long)player.Id.SteamId);
                    }
                }
                //if (LastMessageSent.ContainsKey(allianceChannels[e.Channel.Id]))
                //{
                //    if (LastMessageSent[allianceChannels[e.Channel.Id]].Equals(e.Message.Content))
                //    {
                //        return Task.CompletedTask;
                //    }
                //}
                String[] split = e.Message.Content.Split(':');
                int i = 0;
                if (WorldName.Equals("") && MyMultiplayer.Static.HostName != null)
                {
                    if (MyMultiplayer.Static.HostName.Contains("SENDS"))
                    {
                        WorldName = MyMultiplayer.Static.HostName.Replace("SENDS", "");
                    }
                    else
                    {
                        if (MyMultiplayer.Static.HostName.Equals("Sigma Draconis Lobby"))
                        {
                            WorldName = "01";
                        }
                        else
                        {
                            WorldName = MyMultiplayer.Static.HostName;
                        }
                    }
                }

                String exclusionBeforeFormat = GetStringBetweenCharacters(split[0], '[', ']');
                if (!exclusionBeforeFormat.Contains(WorldName) && !exclusionBeforeFormat.Contains("LOG"))
                {


                    StringBuilder message = new StringBuilder();
                    foreach (String s in split)
                    {
                        if (i == 0)
                        {
                            i++;
                            continue;
                        }
                        message.Append(s);
                    }
                    StringBuilder newMessage = new StringBuilder();
                    string output = e.Message.Content.Substring(e.Message.Content.IndexOf(':') + 1);
                    AllianceChat.SendChatMessageFromDiscord(allianceChannels[e.Channel.Id], split[0].Replace("**", ""), output.Replace("**", "").Trim(), 0);
                }
            }
            else
            {
                if (debugMode)
                {
                    if (MySession.Static.Players.GetPlayerByName("Crunch") != null)
                    {
                        MyPlayer player = MySession.Static.Players.GetPlayerByName("Crunch");
                        ShipyardCommands.SendMessage("Discord 3", "Player message " + e.Message.ChannelId + " " + e.Channel.Id, Color.Blue, (long)player.Id.SteamId);
                    }
                }
                if (WorldName.Equals("") && MyMultiplayer.Static.HostName != null)
                {
                    if (MyMultiplayer.Static.HostName.Contains("SENDS"))
                    {
                        WorldName = MyMultiplayer.Static.HostName.Replace("SENDS", "");
                    }
                    else
                    {
                        if (MyMultiplayer.Static.HostName.Equals("Sigma Draconis Lobby"))
                        {
                            WorldName = "01";
                        }
                        else
                        {
                            WorldName = MyMultiplayer.Static.HostName;
                        }
                    }
                }
                if (debugMode)
                {
                    if (MySession.Static.Players.GetPlayerByName("Crunch") != null)
                    {
                        MyPlayer player = MySession.Static.Players.GetPlayerByName("Crunch");
                        ShipyardCommands.SendMessage("Discord 4", "Player message 1", Color.Blue, (long)player.Id.SteamId);
                    }
                }
                //if (nickNames.ContainsKey(e.Message.Author.Id))
                //{
                //    if (debugMode)
                //    {
                //        if (MySession.Static.Players.GetPlayerByName("Crunch") != null)
                //        {
                //            MyPlayer player = MySession.Static.Players.GetPlayerByName("Crunch");
                //            ShipyardCommands.SendMessage("Discord 5", "Player message 2", Color.Blue, (long)player.Id.SteamId);
                //        }
                //    }
                AllianceChat.SendChatMessageFromDiscord(allianceChannels[e.Channel.Id], "[D] " + e.Message.Author.Username, e.Message.Content.Trim(), e.Author.Id);
                //}
                //else
                //{
                //    if (debugMode)
                //    {
                //        if (MySession.Static.Players.GetPlayerByName("Crunch") != null)
                //        {
                //            MyPlayer player = MySession.Static.Players.GetPlayerByName("Crunch");
                //            ShipyardCommands.SendMessage("Discord 6", "Player message 3", Color.Blue, (long)player.Id.SteamId);
                //        }
                //    }
                //    Task.Run(async () =>
                //    {

                //        String nick;
                //          DiscordMember mem = await e.Guild.GetMemberAsync(e.Author.Id);
                //        nick = mem.Nickname;
                //        if (String.IsNullOrEmpty(nick))
                //        {
                //            nickNames.Add(e.Message.Author.Id, mem.DisplayName);
                //        }
                //        else
                //        {
                //            nickNames.Add(e.Message.Author.Id, nick);
                //        }
                //        AllianceChat.SendChatMessageFromDiscord(allianceChannels[e.Channel.Id], "[D] " + nickNames[e.Message.Author.Id], e.Message.Content.Trim(), e.Author.Id);
                //    });

                //}
                //e.Message.Author.
                //String nick = e.Guild.GetMemberAsync(e.Author.Id).Result.Nickname;
                //if (String.IsNullOrEmpty(nick))
                //{

                //}
                //else
                //{
                //   AllianceChat.SendChatMessageFromDiscord(allianceChannels[e.Channel.Id], "[D] " + nick, e.Message.Content.Trim(), e.Author.Id);
                //}
            }
        }
        else
        {
            if (debugMode)
            {
                if (MySession.Static.Players.GetPlayerByName("Crunch") != null)
                {
                    MyPlayer player = MySession.Static.Players.GetPlayerByName("Crunch");
                    ShipyardCommands.SendMessage("Discord 3", "Message channel not alliance channel " + e.Message.ChannelId + " " + e.Channel.Id, Color.Blue, (long)player.Id.SteamId);
                }
            }
        }
        return Task.CompletedTask;
    }
    private static Task Discord_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
    {

        if (AlliancePlugin.config.DiscordChannelId == e.Channel.Id)
        {
            if (e.Author.IsBot)
            {
                ShipyardCommands.SendMessage(e.Author.Username, e.Message.Content, Color.LightGreen, 0L);
            }
        }


        return Task.CompletedTask;
    }

}
}
