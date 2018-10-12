using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mqtt.LoadBalancer
{
    public class LoadBalancer : IDisposable
    {
        private DynamicBalancerTopic subTopic;
        internal readonly HashSet<string> Groups = new HashSet<string>();
        internal readonly Dictionary<string, TopicBalancer> Balancers = new Dictionary<string, TopicBalancer>();

        public Paths Paths { get; }

        public string ClientId { get; set; }
        public string Address { get; set; }
        public TimeSpan AutoReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);
        public MqttClientOptionsBuilderTlsParameters TlsOptions { get; set; } = new MqttClientOptionsBuilderTlsParameters
        {
            UseTls = false
        };

        public IManagedMqttClient Client { get; private set; }

        public LoadBalancer(Paths paths = null)
        {
            Paths = paths ?? new Paths();

            Client = new MqttFactory().CreateManagedMqttClient();
        }

        public LoadBalancer WithGroupTopics(IEnumerable<GroupTopic> groupTopics)
        {
            foreach(var gt in groupTopics)
            {
                Groups.Add(gt.Group);
                Balancers.Add(gt.Topic, new TopicBalancer(this, gt.Topic, Client));
            }

            return this;
        }

        public LoadBalancer WithDynamicTopicsAndGroups()
        {
            subTopic = new DynamicBalancerTopic(this);
            return this;
        }

        public void RemoveGroup(string group)
        {
            Groups.Remove(group);
        }

        public void RemoveTopic(string topic)
        {
            if (!Balancers.TryGetValue(topic, out var balancer))
                return;

            balancer.Dispose();
            Balancers.Remove(topic);
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

            await Client.StartAsync(moptions);
        }

        /// <summary>
        /// unsubscribes, stops, and disposes the client
        /// </summary>
        public void Dispose()
        {
            Groups.Clear();
            foreach (var kvp in Balancers)
                kvp.Value.Dispose();
            Balancers.Clear();

            Client.StopAsync().Wait();

            Client.Dispose();
        }
    }
}
