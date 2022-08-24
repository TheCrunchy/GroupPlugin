using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using AllianceDiscordController.Models;
using AlliancesPlugin.Alliances;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AllianceDiscordController
{
    public class Program
    {
        public static IModel Channel { get; set; }
        public static string ExchangeName { get; set; } = "GenericJson";

        private static Dictionary<string, Action<string>> Handlers = new Dictionary<string, Action<string>>();
        public static Dictionary<Guid, DiscordClient> Bots = new Dictionary<Guid, DiscordClient>();
        public static Dictionary<ulong, Guid> MappedChannels = new Dictionary<ulong, Guid>();
        public static Dictionary<Guid, DiscordChannel> StoredChannels = new Dictionary<Guid, DiscordChannel>();
        public static void Main(string[] args)
        {
            var config = new Config();
            var factory = new ConnectionFactory();
            if (string.IsNullOrWhiteSpace(config.Username))
            {
                factory.HostName = config.Hostname;
            }
            else
            {
                factory.HostName = config.Hostname;
                factory.UserName = config.Username;
                factory.Password = config.Password;
                factory.Port = config.Port;
            }

            var connection = factory.CreateConnection();
            Channel = connection.CreateModel();

            Channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Fanout);

            var queueName = Channel.QueueDeclare().QueueName;
            Channel.QueueBind(queue: queueName,
                exchange: ExchangeName,
                routingKey: "");

            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += ReceiveMessage;

            Channel.BasicConsume(queue: queueName,
                autoAck: true,
                consumer: consumer);
           Handlers.Add("AllianceMessage", HandleAllianceMessage);
       //     Handlers.Add("AllianceMessage", YEET);
            Console.ReadLine();
        }

        public static void YEET(string message)
        {
            Console.WriteLine(message);
        }
        public static async void HandleAllianceMessage(string JsonMessage)
        {
            var message = JsonConvert.DeserializeObject<AllianceChatMessage>(JsonMessage);
            if (message.FromDiscord)
            {
                return;
            }
            Console.WriteLine($"Sending a message to Discord {message.SenderPrefix} : {message.MessageText}");
            if (Bots.TryGetValue(message.AllianceId, out var discord))
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

                    bot = new DiscordClient(config);
                    bot.ConnectAsync();
                    bot.MessageCreated += Discord_AllianceMessage;
                    Bots.Add(message.AllianceId, bot);
                    if (!MappedChannels.TryGetValue(message.ChannelId, out var ids))
                    {
                        MappedChannels.Add(message.ChannelId, message.AllianceId);
                    }
                 
                    await SendMessage(bot, message);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error setting up {message.AllianceId} {e}");
                }
       
            }
        }

        public static async Task SendMessage(DiscordClient Discord, AllianceChatMessage Message)
        {
            if (StoredChannels.TryGetValue(Message.AllianceId, out var channel))
            {
                var bot = Discord.SendMessageAsync(channel, $"{Message.SenderPrefix} {Message.MessageText.Replace("/n", "\n")}").Result.Author.Id;
            }
            else
            {
                DiscordChannel chann = await Discord.GetChannelAsync(Message.ChannelId);
                var botId = Discord.SendMessageAsync(chann, $"{Message.SenderPrefix} {Message.MessageText.Replace("/n", "\n")}").Result.Author.Id;
                StoredChannels.Add(Message.AllianceId, chann);
            }
      
        }

        public static Task Discord_AllianceMessage(DiscordClient discord, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (e.Author.IsBot)
            {
                return Task.CompletedTask;
            }
            Console.WriteLine($"{DateTime.Now} Discord Message Recieved {e.Message.Author.Username} {e.Message.Content.Trim()}");
            if (MappedChannels.TryGetValue(e.Channel.Id, out Guid id))
            {
                var message = new AllianceChatMessage
                {
                    SenderPrefix = e.Message.Author.Username,
                    MessageText = e.Message.Content.Trim(),
                    AllianceId = id,
                    FromDiscord = true
                };
                SendFromDiscord("AllianceMessage", JsonConvert.SerializeObject(message));
            }
            return Task.CompletedTask;
        }

        public static void ReceiveMessage(object Model, BasicDeliverEventArgs eventArgs)
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var Message = JsonConvert.DeserializeObject<JsonMessage>(json);
                var MessageType = Message.MessageType;
                var MessageBody = Message.MessageBodyJsonString;

                if (!Handlers.TryGetValue(MessageType, out var action)) return;
                action.Invoke(MessageBody);
            }
            catch (Exception ex)
            {
            }
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
            Channel.BasicPublish(exchange: ExchangeName,
                routingKey: "",
                basicProperties: null,
                body: body);
        }

    }
}
