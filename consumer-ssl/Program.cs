using System;
using Confluent.Kafka;

namespace consumer_ssl
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "192.168.56.1:9093",
                GroupId= "consumer-group-ssl",
                ClientId = "consumer-ssl-01",
                SaslMechanism = SaslMechanism.Plain,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslUsername = "admin",
                SaslPassword = "admin",
                SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.None,
                SslCaLocation = @"D:\RepoTest\kafka-ssl\samples\broker-certs\root.crt",
                SslCertificateLocation = @"D:\RepoTest\kafka-ssl\samples\client-certs\consumer_client.crt",
                SslKeyLocation = @"D:\RepoTest\kafka-ssl\samples\client-certs\consumer_client.key"
            };
            
            var builder = new ConsumerBuilder<string, string>(config);
            using (var consumer = builder.Build())
            {
                consumer.Subscribe("test");
                while (true)
                {
                    var record = consumer.Consume(1000);
                    if (record != null)
                        Console.WriteLine($"Key:{record.Message.Key}|Value:{record.Message.Value}");
                }
            }
        }
    }
}
