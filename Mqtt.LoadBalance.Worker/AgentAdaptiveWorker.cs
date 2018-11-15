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
        readonly TopicListener dontWorkTopic;

        volatile string workingUuid;

        public AgentAdaptiveWorker(WorkerManager manager, GroupTopic groupTopic, string workerId)
            :base(manager, groupTopic, workerId)
        {
            reqTopic = new TopicListener(Manager.Paths.GetAvailableReq(Topic), Manager.Client);
            reqTopic.MqttMessageReceived += ReqTopic_MqttMessageReceived;

            workTopic = new TopicListener(Manager.Paths.GetDoWork(Group, WorkerId, Topic), Manager.Client);
            workTopic.MqttMessageReceived += WorkTopic_MqttMessageReceived;

            dontWorkTopic = new TopicListener(Manager.Paths.GetDontWork(Group), Manager.Client);
            dontWorkTopic.MqttMessageReceived += DontWorkTopic_MqttMessageReceived;

            manager.Workers.Add(this);
        }

        private void ReqTopic_MqttMessageReceived(IList<string> wildcards, MqttApplicationMessageReceivedEventArgs msg)
        {
            var uuid = wildcards[0];
            var originalTopic = Manager.Paths.GetRequestTopic(uuid, msg.ApplicationMessage.Topic);
            //a request to do work.
            if (DoWork == null)
                return;

            var canWork = (CanWork?.Invoke(this, originalTopic) ?? true) 
                && (Interlocked.CompareExchange(ref workingUuid, uuid, null) == null);
            if (canWork)
                Manager.Client.PublishAsync(Manager.Paths.GetAvailableResp(Group, WorkerId, uuid, originalTopic));
        }

        private void DontWorkTopic_MqttMessageReceived(IList<string> wildcards, MqttApplicationMessageReceivedEventArgs msg)
        {
            var uuid = wildcards[0];
            var worker = wildcards[1];
            if (worker == WorkerId)
                return; //we were chosen for work, whatever it was.
            if (Interlocked.CompareExchange(ref workingUuid, null, uuid) == uuid)
            {
                //we were told to not work on uuid.
            }
            else
            {
                //told not to work on something we're not waiting for work on.
            }
        }

        private async void WorkTopic_MqttMessageReceived(IList<string> wildcards, MqttApplicationMessageReceivedEventArgs msg)
        {
            var originalTopic = Manager.Paths.GetWorkTopic(Group, WorkerId, msg.ApplicationMessage.Topic);
            try
            {
                await (DoWork?.Invoke(this, wildcards, originalTopic, msg.ApplicationMessage) ?? Task.FromResult(0));
            }
            catch (Exception e)
            {

            }
            finally
            {
                Interlocked.Exchange(ref workingUuid, null);
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
