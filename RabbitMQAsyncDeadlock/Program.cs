using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQAsyncDeadlock
{
    class Program
    {
        static void Main(string[] args)
        {
            var ct = new ConnectionTest();

            ct.StartConsumer("AsyncTestExchange", "TestQueue");

            Console.WriteLine("Press enter to close application");
            Console.ReadLine();
        }
    }

    class ConnectionTest
    {
        private readonly ConnectionFactory _factory;

        public ConnectionTest()
        {
            _factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                AutomaticRecoveryEnabled = true,
                ClientProperties = GetRabbitClientProperties(),
                Port = -1,
                DispatchConsumersAsync = true
            };
        }

        private static Dictionary<string, object> GetRabbitClientProperties()
        {
            var properties = new Dictionary<string, object>();

            var dic = new Dictionary<string, string>
            {
                {"client_api", "async.test"},
                {"host", Dns.GetHostName()},
                {"platform", Dns.GetHostName()},
                {"machine_name", Dns.GetHostName()},
                {"product", Assembly.GetEntryAssembly().ManifestModule.Name},
                {"version", "0.0.0.0"}
            };

            foreach (var entry in dic)
            {
                var bytesProduct = Encoding.UTF8.GetBytes(entry.Value);
                properties[entry.Key] = bytesProduct;
            }

            return properties;
        }

        public void StartConsumer(string exchangeName, string queueName)
        {
            var connection = _factory.CreateConnection("test async");
            var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchangeName, ExchangeType.Topic, true);
            channel.QueueDeclare(queueName, true, false, false, null);
            channel.QueueBind(queueName, exchangeName, "#", null);


            // AsyncEventingBasicConsumer causes timeout when trying to create another Channel on the same Connection... 
            // also if Chanel is reused then QueueDeclare is failing with the timeout.
            // this does not happen with EventingBasicConsumer
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += (sender, args) => OnMessageReceivedAsync(connection, channel, args);
            channel.BasicConsume(queueName, false, consumer);
        }


        private static int _queueSuffix;

        private static async Task OnMessageReceivedAsync(IConnection connection, IModel channel,
            BasicDeliverEventArgs received)
        {
            // uncomment this line out to prevent exception
            // await Task.Yield();
            try
            {
                Console.WriteLine("Message received!");

                // exception happens here!
                var otherChannel = connection.CreateModel();
                otherChannel.QueueDeclare($"test-{Interlocked.Increment(ref _queueSuffix)}", true, false, false, null);
                channel.BasicAck(received.DeliveryTag, false);
                Console.WriteLine("Message consumed!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                channel.BasicNack(received.DeliveryTag, false, true);
            }
        }
    }
}