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
            try
            {
                // Windows Vista - 8.1
                if (Environment.OSVersion.Platform.Equals(PlatformID.Win32NT) && Environment.OSVersion.Version.Major == 6)
                {
                    Discord = new DiscordClient(new DiscordConfiguration
                    {
                        Token = AlliancePlugin.config.DiscordBotToken,
                        TokenType = TokenType.Bot,
                        WebSocketClientFactory = WebSocket4NetClient.CreateNew
                    });
                }
                else
                {
                    Discord = new DiscordClient(new DiscordConfiguration
                    {
                        Token = AlliancePlugin.config.DiscordBotToken,
                        TokenType = TokenType.Bot
                    });
                }
            }
            catch (Exception ex)
            {
                AlliancePlugin.Log.Error(ex);
                Ready = false;
                return Task.CompletedTask;
            }

            Discord.ConnectAsync();

            Discord.MessageCreated += Discord_MessageCreated;
            game = new DiscordActivity();

            Discord.Ready += async e =>
            {
                Ready = true;
                await Task.CompletedTask;
            };


            return Task.CompletedTask;
        }
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
                            WebSocketClientFactory = WebSocket4NetClient.CreateNew
                        });
                    }
                    else
                    {
                        bot = new DiscordClient(new DiscordConfiguration
                        {
                            Token = Encryption.DecryptString(alliance.AllianceId.ToString(), alliance.DiscordToken),
                            TokenType = TokenType.Bot
                        });
                    }
          
                }
                catch (Exception ex)
                {
                    AlliancePlugin.Log.Error(ex);
                    return Task.CompletedTask;
                }
                
                bot.ConnectAsync();

                bot.MessageCreated += Discord_AllianceMessage;
                game = new DiscordActivity();

                bot.Ready += async e =>
                {
                    AllianceReady = true;
            
                    await Task.CompletedTask;
                };

                allianceBots.Remove(alliance.AllianceId);
                allianceChannels.Remove(alliance.DiscordChannelId);
                allianceBots.Add(alliance.AllianceId, bot);
                allianceChannels.Add(channelId, alliance.AllianceId);
            }
            return Task.CompletedTask;
        }
        public static void Stopdiscord()
        {
            if (Ready)
            {
                RunGameTask(() =>
                {
                    DisconnectDiscord().ConfigureAwait(false).GetAwaiter().GetResult();
                });
            }
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


        private static async Task DisconnectDiscord()
        {
            Ready = false;
            AllianceReady = false;
            foreach (DiscordClient bot in allianceBots.Values)
            {
                bot?.DisconnectAsync();
            }
            await Discord?.DisconnectAsync();
        }

        public static void SendMessageToDiscord(string message, KothConfig config)
        {
            if (Ready && config.DiscordChannelId > 0 && config.doDiscordMessages)
            {
                DiscordChannel chann = Discord.GetChannelAsync(config.DiscordChannelId).Result;
                botId = Discord.SendMessageAsync(chann, message.Replace("/n", "\n")).Result.Author.Id;


            }
            else
            {
                if (config.doChatMessages)
                {
                    ShipyardCommands.SendMessage("Territory Capture", message, Color.LightGreen, 0L);
                }
            }
        }

        public static void SendAllianceMessage(Alliance alliance, string prefix, string message)
        {
            if (AllianceReady && alliance.DiscordChannelId > 0)
            {
               
                DiscordClient bot = allianceBots[alliance.AllianceId];
                DiscordChannel chann = bot.GetChannelAsync(alliance.DiscordChannelId).Result;
                if (bot == null)
                {
                    return;
                }
                botId = bot.SendMessageAsync(chann, "**" + prefix + "**:" + message.Replace(" /n", "\n")).Result.Author.Id;
            }
        }

        public static bool AllianceHasBot(Guid id)
        {
            if (allianceBots.ContainsKey(id))
                return true;
            return false;
        }
        public static Dictionary<Guid, string> LastMessageSent = new Dictionary<Guid, string>();
        private static Task Discord_AllianceMessage(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (allianceChannels.ContainsKey(e.Channel.Id))
            {
                if (e.Author.IsBot)
                {
                    //if (LastMessageSent.ContainsKey(allianceChannels[e.Channel.Id]))
                    //{
                    //    if (LastMessageSent[allianceChannels[e.Channel.Id]].Equals(e.Message.Content))
                    //    {
                    //        return Task.CompletedTask;
                    //    }
                    //}
                    String[] split = e.Message.Content.Split(':');
                    int i = 0;
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
                    AllianceChat.SendChatMessageFromDiscord(allianceChannels[e.Channel.Id],split[0].Replace("**", ""), message.ToString(), 0);
                }
                else
                {
                    if (String.IsNullOrEmpty(e.Guild.GetMemberAsync(e.Author.Id).Result.Nickname))
                    {
                        AllianceChat.SendChatMessageFromDiscord(allianceChannels[e.Channel.Id], e.Message.Author.Username, e.Message.Content, e.Author.Id);
                    }
                    else
                    {
                        AllianceChat.SendChatMessageFromDiscord(allianceChannels[e.Channel.Id], e.Guild.GetMemberAsync(e.Author.Id).Result.Nickname, e.Message.Content, e.Author.Id);
                    }
                }
            }
            return Task.CompletedTask;
        }
        private static Task Discord_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            foreach (KothConfig koth in AlliancePlugin.KOTHs)
            {
                if (koth.DiscordChannelId == e.Channel.Id)
                {
                    if (e.Author.IsBot)
                    {
                        ShipyardCommands.SendMessage(e.Author.Username, e.Message.Content, Color.LightGreen, 0L);
                    }
                }

            }
            return Task.CompletedTask;
        }

    }
}
