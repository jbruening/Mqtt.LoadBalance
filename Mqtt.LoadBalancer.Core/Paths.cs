namespace Mqtt.LoadBalancer
{
    public class Paths
    {
        public const string Group = "{grp}";
        public const string Topic = "{top}";
        public const string Uuid = "{uuid}";
        public const string MessageTopic = "{mtop}";
        public const string WorkerId = "{wid}";
        
        /// <summary>
        /// topic the load balancer should subscribe to for dynamically adding new topics/groups
        /// </summary>
        public string WorkerSubs { get; set; } = "lb/sub/+";
        /// <summary>
        /// topic the load balancer should publish on when workers broadcast themselves
        /// </summary>
        public string WorkerSubAck { get; set; } = "lb/suback/" + Group;
        
        /// <summary>
        /// topic that the load balancer should publish on when work is available
        /// </summary>
        public string AvailableReq { get; set; } = "lb/req/" + Uuid + "/" + MessageTopic;
        /// <summary>
        /// topic the load balancer should subscribe to for receiving responses to work available
        /// </summary>
        public string AvailableResp { get; set; } = "lb/rsp/+/+/+/" + Topic;
        /// <summary>
        /// topic the load balancer should publish to to get a worker to start on the original message
        /// </summary>
        public string DoWork { get; set; } = "lb/work/" + Group + "/" + WorkerId + "/" + MessageTopic;
        /// <summary>
        /// topic the load balancer should publish to to tell any workers who said they could work that they shouldn't.
        /// This is published at the same time as the DoWork, but without the message contents of course
        /// </summary>
        public string DontWork { get; set; } = "lb/dwork/" + Uuid + "/" + Group + "/" + WorkerId;

        internal string GetWorkerSubAck(string group)
            => WorkerSubAck
            .Replace(Group, group);

        internal string GetAvailableReq(string uuid, string msgTopic)
            => AvailableReq
            .Replace(Uuid, uuid)
            .Replace(MessageTopic, msgTopic);

        internal string GetAvailableResp(string topic)
            => AvailableResp
            .Replace(Topic, topic);

        internal string GetDoWork(string group, string workerId, string msgTopic)
            => DoWork
            .Replace(Group, group)
            .Replace(WorkerId, workerId)
            .Replace(MessageTopic, msgTopic);

        internal string GetDontWork(string uuid, string group, string workerId)
            => DontWork
            .Replace(Uuid, uuid)
            .Replace(Group, group)
            .Replace(WorkerId, workerId)
    }
}
