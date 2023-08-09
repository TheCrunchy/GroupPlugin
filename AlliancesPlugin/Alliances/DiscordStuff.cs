//using DSharpPlus;
//using DSharpPlus.Entities;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using VRageMath;
//using AlliancesPlugin.KOTH;
//using AlliancesPlugin.Shipyard;
//using Sandbox.Engine.Multiplayer;
//using Sandbox.Game.World;
//using System.IO;
//using VRage.Game.ModAPI;
//using DSharpPlus.EventArgs;
//using AlliancesPlugin.NewCaptureSite;

using System;
using AlliancesPlugin.Shipyard;
using VRageMath;

namespace AlliancesPlugin.Alliances
{
    public class DiscordStuff
    {

        //        do the discord shit in here
        //        private static ulong botId = 0;
        //        public static List<String> errors = new List<string>();
        //        public static bool AllianceReady { get; set; } = false;
        //        public static bool Ready { get; set; } = false;
        //        public static DiscordClient Discord { get; set; }
        //        public static List<ulong> ChannelIds = new List<ulong>();
        //        public static Dictionary<Guid, DiscordClient> allianceBots = new Dictionary<Guid, DiscordClient>();
        //        private static Dictionary<ulong, Guid> allianceChannels = new Dictionary<ulong, Guid>();
        //        public static Task RegisterDiscord()
        //        {
        //            if (!Ready)
        //            {
        //                try
        //                {
        //                    // Windows Vista - 8.1
        //                    if (Environment.OSVersion.Platform.Equals(PlatformID.Win32NT) && Environment.OSVersion.Version.Major == 6)
        //                    {
        //                        Discord = new DiscordClient(new DiscordConfiguration
        //                        {
        //                            Token = AlliancePlugin.config.DiscordBotToken,
        //                            TokenType = TokenType.Bot,
        //                            WebSocketClientFactory = WebSocket4NetClient.CreateNew,
        //                            AutoReconnect = true
        //                        });
        //                    }
        //                    else
        //                    {
        //                        Discord = new DiscordClient(new DiscordConfiguration
        //                        {
        //                            Token = AlliancePlugin.config.DiscordBotToken,
        //                            TokenType = TokenType.Bot,
        //                            AutoReconnect = true
        //                        });
        //                    }
        //                    DiscordConfiguration config = new DiscordConfiguration
        //                    {
        //                        Token = AlliancePlugin.config.DiscordBotToken,
        //                        TokenType = TokenType.Bot,
        //                    };
        //                    Discord = new DiscordClient(config);
        //                }
        //                catch (Exception ex)
        //                {
        //                    AlliancePlugin.Log.Error(ex);
        //                    Ready = false;
        //                    errors.Add(ex.ToString());
        //                    return Task.CompletedTask;
        //                }


        //                try
        //                {
        //                    Discord.ConnectAsync();
        //                }
        //                catch (Exception ex)
        //                {
        //                    Ready = false;
        //                    errors.Add(ex.ToString());
        //                    return Task.CompletedTask;
        //                }
        //                errors.Add("Registering the koth bot");
        //                Discord.MessageCreated += Discord_MessageCreated;
        //                registeredTokens.Add(AlliancePlugin.config.DiscordBotToken);
        //                Ready = true;
        //            }
        //            return Task.CompletedTask;
        //        }
        //        private static Task Client_ClientError(ClientErrorEventArgs e)
        //        {
        //            foreach (KeyValuePair<Guid, DiscordClient> clients in allianceBots)
        //            {
        //                if (e.Client == clients.Value)
        //                {
        //                    Alliance alliance = AlliancePlugin.GetAllianceNoLoading(clients.Key);
        //                    errors.Add("CLIENT ERROR FOR " + alliance.name + " " + e.Exception.StackTrace.ToString());
        //                    return Task.CompletedTask;
        //                }
        //            }
        //            errors.Add("CLIENT ERROR FOR NORMAL BOT " + e.Exception.StackTrace.ToString());
        //            // let's log the details of the error that just 
        //            // occured in our client
        //            //  sender.Logger.LogError(BotEventId, e.Exception, "Exception occured");

        //            // since this method is not async, let's return
        //            // a completed task, so that no additional work
        //            // is done
        //            return Task.CompletedTask;
        //        }
        //        private static Task Client_SocketError(SocketErrorEventArgs e)
        //        {
        //            foreach (KeyValuePair<Guid, DiscordClient> clients in allianceBots)
        //            {
        //                if (e.Client == clients.Value)
        //                {
        //                    Alliance alliance = AlliancePlugin.GetAllianceNoLoading(clients.Key);
        //                    errors.Add("CLIENT SOCKET ERROR FOR " + alliance.name + " " + e.Exception.StackTrace.ToString());
        //                    return Task.CompletedTask;
        //                }
        //            }
        //            errors.Add("CLIENT SOCKET ERROR FOR NORMAL BOT " + e.Exception.StackTrace.ToString());
        //            // let's log the details of the error that just 
        //            // occured in our client
        //            //  sender.Logger.LogError(BotEventId, e.Exception, "Exception occured");

        //            // since this method is not async, let's return
        //            // a completed task, so that no additional work
        //            // is done

        //            return Task.CompletedTask;
        //        }
        //        private static Task Client_SocketClosed(SocketCloseEventArgs e)
        //        {


        //            foreach (KeyValuePair<Guid, DiscordClient> clients in allianceBots)
        //            {
        //                if (e.Client == clients.Value)
        //                {
        //                    Alliance alliance = AlliancePlugin.GetAllianceNoLoading(clients.Key);
        //                    errors.Add("CLOSED FOR " + alliance.name);
        //                    return Task.CompletedTask;
        //                }
        //            }
        //            errors.Add("CLOSED FOR THE NORMAL BOT");
        //            // let's log the details of the error that just 
        //            // occured in our client
        //            //  sender.Logger.LogError(BotEventId, e.Exception, "Exception occured");

        //            // since this method is not async, let's return
        //            // a completed task, so that no additional work
        //            // is done

        //            return Task.CompletedTask;
        //        }

        //        public static List<Guid> registered = new List<Guid>();
        //        public static List<string> temp = new List<string>();

        //        private static List<string> registeredTokens = new List<string>();
        //        private static Dictionary<string, DiscordClient> BotsStoredByTokens = new Dictionary<string, DiscordClient>();
        //        public static Task RegisterAllianceBot(Alliance alliance, ulong channelId)
        //        {

        //            AlliancePlugin.Log.Info($"debug {alliance.name} 1");
        //            var t = Encryption.DecryptString(alliance.AllianceId.ToString(), alliance.DiscordToken);
        //            if (allianceBots.ContainsKey(alliance.AllianceId))
        //            {
        //                return Task.CompletedTask;
        //            }
        //            AlliancePlugin.Log.Info($"debug {t}");
        //            if (BotsStoredByTokens.TryGetValue(t, out DiscordClient discord))
        //            {
        //                AlliancePlugin.Log.Info($"debug {alliance.name} 2");
        //                if (!allianceChannels.ContainsKey(alliance.DiscordChannelId))
        //                {
        //                    allianceChannels.Add(channelId, alliance.AllianceId);
        //                }

        //                allianceBots.Remove(alliance.AllianceId);
        //                allianceBots.Add(alliance.AllianceId, discord);
        //                registered.Add(alliance.AllianceId);
        //                return Task.CompletedTask;
        //            }

        //            if (!allianceBots.ContainsKey(alliance.AllianceId) && Ready)
        //            {
        //                DiscordClient bot;

        //                try
        //                {
        //                    DiscordConfiguration config = new DiscordConfiguration
        //                    {
        //                        Token = Encryption.DecryptString(alliance.AllianceId.ToString(), alliance.DiscordToken),
        //                        TokenType = TokenType.Bot,
        //                    };
        //                    bot = new DiscordClient(config);

        //                }
        //                catch (Exception ex)
        //                {
        //                    errors.Add(ex.ToString());
        //                    AlliancePlugin.Log.Error(ex);
        //                    if (debugMode)
        //                    {
        //                        if (MySession.Static.Players.GetPlayerByName("Crunch") != null)
        //                        {
        //                            MyPlayer player = MySession.Static.Players.GetPlayerByName("Crunch");
        //                            ShipyardCommands.SendMessage("Discord", "token is fucked " + ex.ToString(), Color.Blue, (long)player.Id.SteamId);
        //                        }
        //                    }
        //                    return Task.CompletedTask;
        //                }


        //                try
        //                {
        //                    bot.ConnectAsync();
        //                }
        //                catch (Exception ex)
        //                {
        //                    errors.Add(ex.ToString());
        //                    if (debugMode)
        //                    {
        //                        if (MySession.Static.Players.GetPlayerByName("Crunch") != null)
        //                        {
        //                            MyPlayer player = MySession.Static.Players.GetPlayerByName("Crunch");
        //                            ShipyardCommands.SendMessage("Discord", "Error on connecting " + ex.ToString(), Color.Blue, (long)player.Id.SteamId);
        //                        }
        //                    }
        //                    return Task.CompletedTask;
        //                }

        //                AlliancePlugin.Log.Info($"debug {alliance.name} registered");
        //                temp.Add("Registered " + alliance.name + " BOT");
        //                allianceBots.Remove(alliance.AllianceId);
        //                allianceBots.Add(alliance.AllianceId, bot);
        //                BotsStoredByTokens.Remove(t);
        //                BotsStoredByTokens.Add(t, bot);
        //                allianceChannels.Remove(alliance.DiscordChannelId);
        //                registeredTokens.Add(Encryption.DecryptString(alliance.AllianceId.ToString(), alliance.DiscordToken));
        //                allianceChannels.Add(channelId, alliance.AllianceId);

        //            }

        //            return Task.CompletedTask;
        //        }
        //        private static async void RunGameTask(Action obj)
        //        {
        //            if (AlliancePlugin.TorchBase.CurrentSession != null)
        //            {
        //                await AlliancePlugin.TorchBase.InvokeAsync(obj);
        //            }
        //            else
        //            {
        //                await Task.Run(obj);
        //            }
        //        }

        //        private static string WorldName = "";
        //        public static void DisconnectDiscord()
        //        {
        //            Ready = false;
        //            AllianceReady = false;
        //            foreach (DiscordClient bot in allianceBots.Values)
        //            {
        //                bot.DisconnectAsync();
        //            }
        //            Discord?.DisconnectAsync();
        //        }
        public static void SendMessageToDiscord(string name, string messageText, NewCaptureSite.CaptureSite config,
            Boolean Embed = true)
        {

            var message = new AllianceSendToDiscord
            {
                MessageText = messageText,
                SenderPrefix = name,
                SendToIngame = true,
                DoEmbed = Embed,
                EmbedB = config.FDiscordB,
                EmbedG = config.FDiscordG,
                EmbedR = config.FDiscordR,
                ChannelId = config.AllianceSite
                    ? AlliancePlugin.config.DiscordChannelId
                    : config.FactionDiscordChannelId
            };

            AlliancePlugin.SendToMQ(MQPatching.MQPluginPatch.AllianceSendToDiscord, message);
        }
    }
}
//        public static void SendMessageToDiscord(string message)
//        {
//            if (Ready && AlliancePlugin.config.DiscordChannelId > 0)
//            {
//                DiscordChannel chann = Discord.GetChannelAsync(AlliancePlugin.config.DiscordChannelId).Result;
//                botId = Discord.SendMessageAsync(chann, message.Replace("/n", "\n")).Result.Author.Id;


//            }
//        }
//        public static void SendEmbedToDiscord(string name, string message)
//        {
//            if (Ready && AlliancePlugin.config.DiscordChannelId > 0)
//            {
//                DiscordChannel chann = Discord.GetChannelAsync(AlliancePlugin.config.DiscordChannelId).Result;

//                var embed = new DiscordEmbedBuilder
//                {
//                    Title = "Capture Alert",
//                    Description = "Test Embed",
//                    Color = new DiscordColor(255, 255, 255)

//                };
//                chann.SendMessageAsync(embed);
//            }
//        }
//        private static int attempt = 0;

//        public static void SendAllianceMessage(Alliance alliance, string prefix, string message)
//        {
//            if (AllianceHasBot(alliance.AllianceId) && alliance.DiscordChannelId > 0)
//            {

//                DiscordClient bot = allianceBots[alliance.AllianceId];

//                DiscordChannel chann = bot.GetChannelAsync(alliance.DiscordChannelId).Result;
//                if (bot == null)
//                {
//                    return;
//                }
//                if (WorldName.Equals("") && MyMultiplayer.Static.HostName != null)
//                {
//                    if (MyMultiplayer.Static.HostName.Contains("SENDS"))
//                    {

//                        WorldName = MyMultiplayer.Static.HostName.Replace("SENDS", "");
//                    }
//                    else
//                    {
//                        if (MyMultiplayer.Static.HostName.Equals("Sigma Draconis Lobby"))
//                        {
//                            WorldName = "01";
//                        }
//                        else
//                        {
//                            WorldName = MyMultiplayer.Static.HostName;
//                        }

//                    }
//                }
//                try
//                {
//                    botId = bot.SendMessageAsync(chann, "**[" + WorldName + "] " + prefix + "**: " + message.Replace(" /n", "\n")).Result.Author.Id;
//                    bot.MessageCreated -= Discord_AllianceMessage;
//                    bot.MessageCreated += Discord_AllianceMessage;


//                }
//                catch (DSharpPlus.Exceptions.RateLimitException)
//                {
//                    if (attempt <= 5)
//                    {
//                        attempt++;
//                        SendAllianceMessage(alliance, prefix, message);
//                        attempt = 0;
//                    }
//                    else
//                    {
//                        attempt = 0;
//                    }
//                }
//                catch (System.Net.Http.HttpRequestException)
//                {
//                }
//            }
//            else
//            {
//                if (debugMode)
//                {
//                    if (MySession.Static.Players.GetPlayerByName("Crunch") != null)
//                    {
//                        MyPlayer player = MySession.Static.Players.GetPlayerByName("Crunch");
//                        ShipyardCommands.SendMessage("Discord", "doesnt have bot or channel id is 0", Color.Blue, (long)player.Id.SteamId);
//                    }
//                }
//            }
//        }


//        public static string MakeLog(Alliance alliance, int max)
//        {
//            BankLog log = alliance.GetLog();
//            StringBuilder sb = new StringBuilder();
//            log.log.Reverse();
//            int i = 0;
//            sb.AppendLine("Time,Player,Action,Faction/Player,Amount,NewBalance");
//            foreach (BankLogItem item in log.log)
//            {
//                i++;
//                if (i >= max)
//                {
//                    return sb.ToString();
//                }
//                if (item.FactionPaid > 0)
//                {
//                    IMyFaction fac = MySession.Static.Factions.TryGetFactionById(item.FactionPaid);
//                    if (fac != null)
//                    {
//                        sb.AppendLine(item.TimeClaimed.ToString() + "," + AlliancePlugin.GetPlayerName(item.SteamId) + "," + item.Action + "," + fac.Tag + " " + item.Amount + "," + item.BankAmount);
//                    }
//                    else
//                    {
//                        sb.AppendLine(item.TimeClaimed.ToString() + "," + AlliancePlugin.GetPlayerName(item.SteamId) + "," + item.Action + " a now dead faction ," + item.Amount + "," + item.BankAmount);
//                    }
//                    continue;
//                }
//                if (item.PlayerPaid > 0)
//                {
//                    sb.AppendLine(item.TimeClaimed.ToString() + "," + AlliancePlugin.GetPlayerName(item.SteamId) + "," + item.Action + "," + AlliancePlugin.GetPlayerName(item.PlayerPaid) + ", " + item.Amount + "," + item.BankAmount);
//                }
//                else
//                {

//                    sb.AppendLine(item.TimeClaimed.ToString() + "," + AlliancePlugin.GetPlayerName(item.SteamId) + "," + item.Action + "," + item.Amount + "," + item.BankAmount);
//                }
//            }
//            return sb.ToString();
//        }
//        public static async void SendAllianceLog(Alliance alliance, int max)
//        {
//            if (AllianceHasBot(alliance.AllianceId) && alliance.DiscordChannelId > 0)
//            {

//                DiscordClient bot = allianceBots[alliance.AllianceId];

//                DiscordChannel chann = bot.GetChannelAsync(alliance.DiscordChannelId).Result;
//                if (bot == null)
//                {
//                    return;
//                }


//                String output = await Task.Run(() => MakeLog(alliance, max));



//                File.WriteAllText(AlliancePlugin.path + "//temp.txt", output);
//                FileStream stream = new FileStream(AlliancePlugin.path + "//temp.txt", FileMode.Open);

//                chann.SendFileAsync("LOG.txt", stream);


//            }
//        }

//        public static bool AllianceHasBot(Guid id)
//        {
//            if (allianceBots.ContainsKey(id))
//                return true;
//            return false;
//        }

//        public static string GetStringBetweenCharacters(string input, char charFrom, char charTo)
//        {
//            int posFrom = input.IndexOf(charFrom);
//            if (posFrom != -1) //if found char
//            {
//                int posTo = input.IndexOf(charTo, posFrom + 1);
//                if (posTo != -1) //if found char
//                {
//                    return input.Substring(posFrom + 1, posTo - posFrom - 1);
//                }
//            }

//            return string.Empty;
//        }
//        public static Dictionary<ulong, string> nickNames = new Dictionary<ulong, string>();
//        public static Boolean debugMode = false;
//        public static Dictionary<Guid, string> LastMessageSent = new Dictionary<Guid, string>();

//        static bool tried = false;
//        public static DateTime nextMention = DateTime.Now;
//        public static Task Discord_MessageCreated(DiscordClient discord, DSharpPlus.EventArgs.MessageCreateEventArgs e)
//        {
//            if (WorldName.Equals("") && MyMultiplayer.Static.HostName != null)
//            {
//                if (MyMultiplayer.Static.HostName.Contains("SENDS"))
//                {
//                    WorldName = MyMultiplayer.Static.HostName.Replace("SENDS", "");
//                }
//                else
//                {
//                    if (MyMultiplayer.Static.HostName.Equals("Sigma Draconis Lobby"))
//                    {
//                        WorldName = "01";
//                    }
//                    else
//                    {
//                        WorldName = MyMultiplayer.Static.HostName;
//                    }
//                }
//            }
//            if (e == null)
//            {
//                errors.Add("Null exception? " + DateTime.Now.ToString());
//                if (DateTime.Now >= nextMention)
//                {
//                    DiscordChannel chann = e.Client.GetChannelAsync(AlliancePlugin.config.DiscordChannelId).Result;



//                    botId = e.Client.SendMessageAsync(chann, "**[" + WorldName + "] " + "It happened again Null exception ").Result.Author.Id;
//                    nextMention = DateTime.Now.AddHours(1);
//                }
//                return Task.CompletedTask;
//            }
//            if (e.Message == null)
//            {

//                errors.Add("Null message? " + DateTime.Now.ToString());
//                if (DateTime.Now >= nextMention)
//                {
//                    DiscordChannel chann = e.Client.GetChannelAsync(AlliancePlugin.config.DiscordChannelId).Result;



//                    botId = e.Client.SendMessageAsync(chann, "**[" + WorldName + "] " + "It happened again Null message ").Result.Author.Id;
//                    nextMention = DateTime.Now.AddHours(1);
//                }
//                return Task.CompletedTask;
//            }
//            if (e.Message.Channel == null)
//            {
//                errors.Add("Null channel? " + DateTime.Now.ToString());

//                if (DateTime.Now >= nextMention)
//                {
//                    tried = true;
//                    DiscordChannel chann = e.Client.GetChannelAsync(AlliancePlugin.config.DiscordChannelId).Result;

//                    e.Client.ReconnectAsync();

//                    botId = e.Client.SendMessageAsync(chann, "**[" + WorldName + "] " + "It happened again Null channel ").Result.Author.Id;
//                    nextMention = DateTime.Now.AddHours(1);
//                }
//                return Task.CompletedTask;
//            }
//            if (e.Message.Content.Equals("Connection Check"))
//            {
//                DiscordChannel chann = discord.GetChannelAsync(AlliancePlugin.config.DiscordChannelId).Result;

//                discord.SendMessageAsync(chann, "**[" + WorldName + "] " + "Connected");
//            }
//            if (e.Message.Content.Contains("Checking Server "))
//            {
//                if (e.Message.Content.Equals("Checking Server " + MyMultiplayer.Static.HostName))
//                {
//                    DiscordChannel chann = discord.GetChannelAsync(AlliancePlugin.config.DiscordChannelId).Result;

//                    discord.SendMessageAsync(chann, "**[" + WorldName + "] " + "Connected");
//                }
//                else
//                {

//                    return Task.CompletedTask;
//                }
//            }
//            if (tried)
//            {
//                DiscordChannel chann = discord.GetChannelAsync(AlliancePlugin.config.DiscordChannelId).Result;
//                tried = false;

//                discord.SendMessageAsync(chann, "**[" + WorldName + "] " + "Bot reconnected properly? ");
//            }
//            try
//            {
//                if (e.Author.IsBot)
//                {


//                    if (AlliancePlugin.config.DiscordChannelId == e.Message.Channel.Id)
//                    {
//                        if (e.Message.Embeds.Count > 0)
//                        {
//                            foreach (DiscordEmbed embed in e.Message.Embeds)
//                            {
//                                ShipyardCommands.SendMessage(embed.Title, embed.Description, Color.LightGreen, 0L);
//                            }
//                        }
//                        else
//                        {
//                            ShipyardCommands.SendMessage(e.Author.Username, e.Message.Content, Color.LightGreen, 0L);
//                        }
//                        return Task.CompletedTask;
//                    }



//                    foreach (CaptureSite site in AlliancePlugin.sites)
//                    {
//                        if (!site.AllianceSite)
//                        {
//                            if (site.FactionDiscordChannelId == e.Message.Channel.Id)
//                            {
//                                if (e.Message.Embeds.Count > 0)
//                                {
//                                    foreach (DiscordEmbed embed in e.Message.Embeds)
//                                    {
//                                        ShipyardCommands.SendMessage(embed.Title, embed.Description, Color.LightGreen, 0L);
//                                    }
//                                }
//                                else
//                                {
//                                    ShipyardCommands.SendMessage(e.Author.Username, e.Message.Content, Color.LightGreen, 0L);
//                                }


//                                return Task.CompletedTask;
//                            }
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                errors.Add("Error reading normal discord message " + ex.ToString());

//            }


//            return Task.CompletedTask;
//        }
//    }
//}
