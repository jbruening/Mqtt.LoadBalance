using MQTTnet;
using System.Collections.Generic;

namespace Mqtt.LoadBalancer
{
    class LoadSubscriberTopic : TopicListener
    {
        public LoadBalancer Balancer { get; }

        public LoadSubscriberTopic(LoadBalancer balancer)
            : base("lb/sub/+/#", balancer.Client)
        {
            Balancer = balancer;
        }

        protected override void MqttMessageReceived(IList<string> wildcards, MqttApplicationMessageReceivedEventArgs e)
        {
            if (!Balancer.ListenForWorkers)
                return;

            var group = wildcards[0];
            var topic = wildcards[1];

            Balancer.Groups.Add(group);

            if (!Balancer.Balancers.TryGetValue(topic, out var btopic))
                btopic = new TopicBalancer(Balancer, topic, Client);

            btopic.SubAck(group);
        }
    }
}
