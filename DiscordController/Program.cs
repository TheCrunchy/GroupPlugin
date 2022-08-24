using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Timers;
using AllianceDiscordController.Models;
using AlliancesPlugin;
using AlliancesPlugin.Alliances;
using DiscordController.Handlers;
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
        public static Dictionary<String, DiscordClient> UsedTokens = new Dictionary<String, DiscordClient>();
        public static Dictionary<ulong, Guid> MappedChannels = new Dictionary<ulong, Guid>();
        public static Dictionary<ulong, DiscordChannel> StoredChannels = new Dictionary<ulong, DiscordChannel>();
        public static Config config;

        public static void Main(string[] args)
        { 
            config = new Config();
            var directory = Directory.GetCurrentDirectory();
            var utils = new FileUtils();
            var path = $"{directory}//config.xml";
            if (!File.Exists(path))
            {
                utils.WriteToXmlFile(path, config);
            }
            else
            {
                config = utils.ReadFromXmlFile<Config>(path);
            }

            if (config.UseSeHostingWatchdog)
            {
                 var Timer = new Timer();
                 Timer.Interval = 30000;
                 Timer.Enabled = true;
                 Timer.Elapsed += OnTimedEvent;
            }

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

            try
            {
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
                Handlers.Add("AllianceMessage", AllianceChatHandler.HandleAllianceMessage);
                Handlers.Add("AllianceSendToDiscord", SendOtherToDiscordHandler.SendToDiscord);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not connect to specified values, if this is first run, a config file should have generated at {path}");
                Console.ReadLine();
            }
            Console.ReadLine();
        }
        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            var now = DateTime.Now.ToString("o");
            File.WriteAllText($"{config.PathForWatchdog}//ALLIANCEDISCORD", now);
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
    }
}
