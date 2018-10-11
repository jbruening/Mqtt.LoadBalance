using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Mqtt.LoadBalancer
{
    class TopicBalancer : IDisposable
    {
        class OriginalTopic : TopicListener
        {
            private readonly TopicBalancer listener;

            public OriginalTopic(TopicBalancer listener, string topic)
                : base(topic, listener.Balancer.Client)
            {
                this.listener = listener;
            }

            protected override void MqttMessageReceived(IList<string> wildcards, MqttApplicationMessageReceivedEventArgs e)
            {
                listener.OriginalTopicMessage(wildcards, e);
            }
        }

        class CanWorkTopic : TopicListener
        {
            private readonly TopicBalancer listener;

            public CanWorkTopic(TopicBalancer listener, string topic)
                : base(topic, listener.Balancer.Client)
            {
                this.listener = listener;
            }

            protected override void MqttMessageReceived(IList<string> wildcards, MqttApplicationMessageReceivedEventArgs e)
            {
                listener.CanWorkMessage(wildcards, e);
            }
        }

        public LoadBalancer Balancer { get; }

        private readonly OriginalTopic originalTopic;
        private readonly CanWorkTopic canWorkTopic;

        private readonly ConcurrentDictionary<Guid, Dictionary<string, TaskCompletionSource<string>>> requests
            = new ConcurrentDictionary<Guid, Dictionary<string, TaskCompletionSource<string>>>();

        public TopicBalancer(LoadBalancer balancer, string topic, IManagedMqttClient client)
        {
            Balancer = balancer;
            originalTopic = new OriginalTopic(this, topic);
            canWorkTopic = new CanWorkTopic(this, $"lb/rsp/+/+/{topic}");
        }

        internal void SubAck(string group)
        {
            Balancer.Client.PublishAsync(Balancer.Paths.GetWorkerSubAck(group), originalTopic.Topic);
        }

        async void OriginalTopicMessage(IList<string> wildcards, MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic;
            var dict = Balancer.Groups.ToDictionary(k => k, v => new TaskCompletionSource<string>());
            var guid = Guid.NewGuid();
            var guidStr = guid.ToString("N");

            try
            {
                if (!requests.TryAdd(guid, dict))
                    throw new Exception("guid already exists??");

                //Ask for who can work
                await Task.WhenAll(dict.Select(async kvp => 
                {
                    var group = kvp.Key;
                    var tcs = kvp.Value;

                    Task.Delay(5000).ContinueWith(t => tcs.TrySetCanceled());

                    await Balancer.Client.PublishAsync($"lb/req/{group}/{e.ApplicationMessage.Topic}", guidStr).ConfigureAwait(false);
                    try
                    {
                        var workerId = await tcs.Task.ConfigureAwait(false);
                        var msg = new MqttApplicationMessage
                        {
                            Topic = $"lb/work/{group}/{workerId}/{topic}",
                            Payload = e.ApplicationMessage.Payload,
                            QualityOfServiceLevel = e.ApplicationMessage.QualityOfServiceLevel,
                            Retain = e.ApplicationMessage.Retain
                        };
                        await Balancer.Client.PublishAsync(msg).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        //todo: log ex
                    }
                }));             
            }
            finally
            {
                requests.TryRemove(guid, out var _);
            }
        }

        void CanWorkMessage(IList<string> wildcards, MqttApplicationMessageReceivedEventArgs e)
        {
            var group = wildcards[0];
            var workerId = wildcards[1];
            var loadStr = $"lb/req/{group}/{workerId}/";
            var realTopic = e.ApplicationMessage.Topic.Substring(loadStr.Length);
            var guid = Guid.ParseExact(e.ApplicationMessage.ConvertPayloadToString(), "N");

            if (requests.TryGetValue(guid, out var dict))
            {
                if (dict.TryGetValue(group, out var tcs))
                    tcs.TrySetResult(workerId);
            }
        }

        public void Dispose()
        {
            originalTopic.Dispose();
            canWorkTopic.Dispose();
        }
    }
}
