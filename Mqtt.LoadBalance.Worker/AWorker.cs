using MQTTnet;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mqtt.LoadBalance.Worker
{
    public abstract class AWorker : IDisposable
    {
        public WorkerManager Manager { get; }

        public string WorkerId { get; }
        public string Group { get; }
        public string Topic { get; }

        protected bool Disposed { get; private set; }

        protected AWorker(WorkerManager manager, GroupTopic groupTopic, string workerId)
        {
            Manager = manager;
            Group = groupTopic.Group;
            Topic = groupTopic.Topic;
            WorkerId = workerId;

            if (groupTopic.Dynamic)
            {
                //todo: deal with dynamic groups/topics
                DynamicSubLoop();
            }
        }

        private async void DynamicSubLoop()
        {
            await Task.Delay(0).ConfigureAwait(false);

            while (!Disposed)
            {
                try
                {
                    //workers pub to lb/sub/{group} with {topic} as payload, occasionally as a 'load balancer keepalive'
                    await Manager.Client.PublishAsync(Manager.Paths.GetWorkerSubs(Group), Topic,
                       MqttQualityOfServiceLevel.AtMostOnce);

                    //todo: wait for suback
                    //workers notice that load balancer is available. workers subscribe to paths based on their load balancing type

                    await Task.Delay(5000);
                }
                catch (Exception)
                {

                }
            }
        }

        public virtual void Dispose()
        {
            Disposed = true;
        }
    }
}
