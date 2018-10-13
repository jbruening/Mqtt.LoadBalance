using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mqtt.LoadBalance.Worker
{
    public class WorkerManager
    {
        public Paths Paths { get; }

        public string ClientId { get; set; }
        public string Address { get; set; }
        public TimeSpan AutoReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);
        public MqttClientOptionsBuilderTlsParameters TlsOptions { get; set; } = new MqttClientOptionsBuilderTlsParameters
        {
            UseTls = false
        };

        public IManagedMqttClient Client { get; private set; }

        internal readonly List<AWorker> Workers = new List<AWorker>();

        public WorkerManager(Paths paths = null)
        {
            Paths = paths ?? new Paths();

            Client = new MqttFactory().CreateManagedMqttClient();
        }

        public WorkerManager WithWorker(AWorker worker)
        {
            Workers.Add(worker);
            return this;
        }

        public WorkerManager WithWorkers(IEnumerable<AWorker> workers)
        {
            foreach (var w in workers)
                Workers.Add(w);

            return this;
        }

        /// <summary>
        /// this just sets up the options and then runs Client.StartAsync(options)
        /// 
        /// If you don't like the MqttClientOptions options this starts with, then you could set up the options and start the client yourself
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync()
        {
            if (string.IsNullOrWhiteSpace(Address))
                throw new InvalidOperationException("No address specified");

            var options = new MqttClientOptionsBuilder()
                    .WithClientId(ClientId ?? (ClientId = Guid.NewGuid().ToString("N")))
                    .WithTcpServer(Address)
                    .WithTls(TlsOptions);

            var moptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(AutoReconnectDelay)
                .WithClientOptions(options)
                .Build();

            Client.ApplicationMessageReceived += (s, e) => System.Diagnostics.Debug.WriteLine($"recv {e.ApplicationMessage.Topic}");

            await Client.StartAsync(moptions);
        }
    }
}
