using System;
using Confluent.Kafka;

namespace producer_ssl
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "192.168.56.1:9093",
                ClientId = "producer-ssl-01",
                SaslMechanism = SaslMechanism.Plain,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslUsername = "admin",
                SaslPassword = "admin",
                SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.None,
                SslCaLocation = @"root.crt",
                SslCertificateLocation = @"producer_client.crt",
                SslKeyLocation = @"producer_client.key"
            };

            var builder = new ProducerBuilder<string, string>(config);
            string line = "";
            using (var producer = builder.Build())
            {
                var rd = new Random();
                while (!line.Contains("exit", StringComparison.InvariantCultureIgnoreCase))
                {
                    producer.Produce("test", new Message<string, string>{
                        Key= $"{rd.Next(0,100)}",
                        Value = $"{rd.Next(0,10)}-{rd.Next(0,100)}"
                    });
                    line = Console.ReadLine();
                }
            }
        }
    }
}
