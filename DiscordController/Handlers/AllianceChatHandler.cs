using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AllianceDiscordController;
using AllianceDiscordController.Models;
using AlliancesPlugin.Alliances;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace DiscordController.Handlers
{
    public static class AllianceChatHandler
    {
        public static async void HandleAllianceMessage(string JsonMessage)
        {
            var message = JsonConvert.DeserializeObject<AllianceChatMessage>(JsonMessage);
            if (message.FromDiscord)
            {
                return;
            }

            if (message.SenderPrefix is not "Init")
            {
                Console.WriteLine($"{DateTime.Now} Sending a message to Discord {message.AllianceId} {message.SenderPrefix} : {message.MessageText}");
            }
            if (Program.Bots.TryGetValue(message.AllianceId, out var discord))
            {
                await SendMessage(discord, message);
            }
            else
            {
                try
                {
                    var token = Encryption.DecryptString(message.AllianceId.ToString(), message.BotToken);
                    DiscordClient bot;
                    DiscordConfiguration config = new DiscordConfiguration
                    {
                        Token = token,
                        TokenType = TokenType.Bot,
                    };
                    if (Program.UsedTokens.TryGetValue(token, out var inUse))
                    {
                        Program.Bots.Add(message.AllianceId, inUse);
                        await SendMessage(inUse, message);
                    }
                    else
                    {
                        bot = new DiscordClient(config);
                        Program.UsedTokens.Add(token, bot);
                        await bot.ConnectAsync();
                        bot.MessageCreated += Discord_AllianceMessage;
                        await SendMessage(bot, message);
                    }

                    if (!Program.MappedChannels.TryGetValue(message.ChannelId, out var ids))
                    {
                        Program.MappedChannels.Add(message.ChannelId, message.AllianceId);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error setting up {message.AllianceId} {e}");
                }

            }
        }

        public static async Task SendMessage(DiscordClient Discord, AllianceChatMessage Message)
        {
            if (Message.SenderPrefix is  "Init")
            {
                return;
            }
            if (Program.StoredChannels.TryGetValue(Message.ChannelId, out var channel))
            {
                var bot = Discord.SendMessageAsync(channel, $"{Message.SenderPrefix} {Message.MessageText.Replace("/n", "\n")}").Result.Author.Id;
            }
            else
            {
                DiscordChannel chann = await Discord.GetChannelAsync(Message.ChannelId);
                var botId = Discord.SendMessageAsync(chann, $"{Message.SenderPrefix} {Message.MessageText.Replace("/n", "\n")}").Result.Author.Id;
                Program.StoredChannels.Add(Message.ChannelId, chann);
            }

        }

        public static Task Discord_AllianceMessage(DiscordClient discord, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (e.Author.IsBot)
            {
                return Task.CompletedTask;
            }
  
            if (!Program.MappedChannels.TryGetValue(e.Channel.Id, out var id)) return Task.CompletedTask;
            var message = new AllianceChatMessage
            {

                SenderPrefix = e.Message.Author.Username,
                MessageText = e.Message.Content.Trim(),
                AllianceId = id,
                FromDiscord = true
            };
            Console.WriteLine($"{DateTime.Now} Discord Message {e.Message.Author.Username} {e.Message.Content.Trim()}");
            SendFromDiscord("AllianceMessage", JsonConvert.SerializeObject(message));
            return Task.CompletedTask;
        }

        public static void SendFromDiscord(string MessageType, string JsonMessage)
        {
            var message = new JsonMessage()
            {
                MessageType = MessageType,
                MessageBodyJsonString = JsonMessage
            };

            var json = JsonConvert.SerializeObject(message);

            var body = Encoding.UTF8.GetBytes(json);
            Program.Channel.BasicPublish(exchange: Program.ExchangeName,
                routingKey: "",
                basicProperties: null,
                body: body);
        }

    }
}
