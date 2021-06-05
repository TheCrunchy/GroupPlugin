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

        public static bool Ready { get; set; } = false;
        public static DiscordClient Discord { get; set; }
        public static List<ulong> ChannelIds = new List<ulong>();

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
            catch (Exception ex) {
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
