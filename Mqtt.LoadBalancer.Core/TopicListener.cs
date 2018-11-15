using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mqtt.LoadBalancer
{
    public abstract class TopicListener : IDisposable
    {
        protected IManagedMqttClient Client { get; }
        private readonly Regex regex;

        public string Topic { get; }

        public TopicListener(string topic, IManagedMqttClient client)
        {
            Client = client;
            Topic = topic;

            regex = LoadBalance.MqttRegex.TopicRegex(Topic);

            Debug.WriteLine($"sub {topic}");
            client.ApplicationMessageReceived += Client_ApplicationMessageReceived;
            client.SubscribeAsync(Topic);
        }

        private void Client_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            var match = regex.Match(e.ApplicationMessage.Topic);
            if (!match.Success)
                return;

            Debug.WriteLine($"recv {e.ApplicationMessage.Topic}");
            MqttMessageReceived(match.Groups.Cast<Group>().Skip(1).Select(o => o.Value).ToList(), e);
        }

        protected abstract void MqttMessageReceived(IList<string> wildcards, MqttApplicationMessageReceivedEventArgs e);

        public void Dispose()
        {
            Client.ApplicationMessageReceived -= Client_ApplicationMessageReceived;
            Client.UnsubscribeAsync(Topic);
        }
    }
}
