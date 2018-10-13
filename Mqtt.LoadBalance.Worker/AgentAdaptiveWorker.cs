using MQTTnet;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mqtt.LoadBalance.Worker
{
    public delegate bool WorkAvailable(AgentAdaptiveWorker sender, string topic);
    public delegate Task DoWork(AgentAdaptiveWorker sender, IList<string> wildcards, string topic, MqttApplicationMessage msg);

    public class AgentAdaptiveWorker: AWorker
    {
        public event WorkAvailable CanWork;
        public event DoWork DoWork;

        readonly TopicListener reqTopic;
        readonly TopicListener workTopic;

        volatile int working;

        public AgentAdaptiveWorker(WorkerManager manager, GroupTopic groupTopic, string workerId)
            :base(manager, groupTopic, workerId)
        {
            reqTopic = new TopicListener(Manager.Paths.GetAvailableReq(Topic), Manager.Client);
            reqTopic.MqttMessageReceived += ReqTopic_MqttMessageReceived;

            workTopic = new TopicListener(Manager.Paths.GetDoWork(Group, WorkerId, Topic), Manager.Client);
            workTopic.MqttMessageReceived += WorkTopic_MqttMessageReceived;
        }

        private void ReqTopic_MqttMessageReceived(IList<string> wildcards, MqttApplicationMessageReceivedEventArgs msg)
        {
            var uuid = wildcards[0];
            var originalTopic = Manager.Paths.GetRequestTopic(uuid, msg.ApplicationMessage.Topic);
            //a request to do work.
            var canWork = CanWork?.Invoke(this, originalTopic) ?? (DoWork != null) && working == 0;
            if (canWork)
                Manager.Client.PublishAsync(Manager.Paths.GetAvailableResp(Group, WorkerId, uuid, originalTopic));
        }

        private async void WorkTopic_MqttMessageReceived(IList<string> wildcards, MqttApplicationMessageReceivedEventArgs msg)
        {
            var originalTopic = Manager.Paths.GetWorkTopic(Group, WorkerId, msg.ApplicationMessage.Topic);

            Interlocked.Increment(ref working);
            try
            {
                await (DoWork?.Invoke(this, wildcards, originalTopic, msg.ApplicationMessage) ?? Task.FromResult(0));
            }
            catch (Exception e)
            {

            }
            finally
            {
                Interlocked.Decrement(ref working);
            }
        }

        /// <summary>
        /// shut down the worker (stop listening/responding to work requests)
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            Manager.Workers.Remove(this);

            reqTopic.Dispose();
            workTopic.Dispose();
        }
    }
}
