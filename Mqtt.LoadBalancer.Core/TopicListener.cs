using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
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

            regex = TopicMatcher(Topic);

            client.ApplicationMessageReceived += Client_ApplicationMessageReceived;
            client.SubscribeAsync(Topic);
        }

        private void Client_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            var match = regex.Match(e.ApplicationMessage.Topic);
            if (!match.Success)
                return;

            MqttMessageReceived(match.Groups.Cast<Group>().Skip(1).Select(o => o.Value).ToList(), e);
        }

        protected abstract void MqttMessageReceived(IList<string> wildcards, MqttApplicationMessageReceivedEventArgs e);

        public static Regex TopicMatcher(string sub)
        {
            sub = sub.Replace("/", "\\/") //escape slashes
                .Replace("+", "(.+?)") //escape and capture single level wildcards
                .Replace("#", "(.+)") //escape and capture multi-level wildcards
                .Trim(); //no white characters
            if (sub.EndsWith("(.+?)")) //last single-level wildcard breaks if there's no slash, so fix it.
                sub = sub.Substring(0, sub.Length - 5) + "(.+)";

            return new Regex(sub);
        }

        public void Dispose()
        {
            Client.ApplicationMessageReceived -= Client_ApplicationMessageReceived;
            Client.UnsubscribeAsync(Topic);
        }
    }
}
