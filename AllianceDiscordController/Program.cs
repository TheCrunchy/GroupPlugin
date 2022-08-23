using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using AllianceDiscordController.Models;
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
            Console.ReadLine();
        }

        public static void HandleAllianceMessage(string JsonMessage)
        {

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

                if (Handlers.TryGetValue(MessageType, out var action))
                {
                    action.Invoke(MessageBody);
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
