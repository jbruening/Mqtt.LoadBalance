using MQTTnet;
using System.Collections.Generic;

namespace Mqtt.LoadBalancer
{
    class DynamicBalancerTopic : TopicListener
    {
        public LoadBalancer Balancer { get; }

        public DynamicBalancerTopic(LoadBalancer balancer)
            : base(balancer.Paths.WorkerSubs, balancer.Client)
        {
            Balancer = balancer;
        }

        protected override void MqttMessageReceived(IList<string> wildcards, MqttApplicationMessageReceivedEventArgs e)
        {
            var group = wildcards[0];
            var topic = e.ApplicationMessage.ConvertPayloadToString();

            Balancer.Groups.Add(group);

            if (!Balancer.Balancers.TryGetValue(topic, out var btopic))
                btopic = new TopicBalancer(Balancer, topic, Client);

            btopic.SubAck(group);
        }
    }
}
