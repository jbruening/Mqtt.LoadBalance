namespace Mqtt.LoadBalancer
{
    public class Paths
    {
        public const string Group = "{grp}";
        public const string Topic = "{top}";
        public const string WorkerId = "{wid}";
        
        /// <summary>
        /// topic the load balancer should subscribe to for dynamically adding new topics/groups
        /// </summary>
        public string WorkerSubs { get; set; } = "lb/sub/+/#";
        /// <summary>
        /// topic the load balancer should publish on when workers broadcast themselves
        /// </summary>
        public string WorkerSubAck { get; set; } = "lb/suback/" + Group;
        
        /// <summary>
        /// topic that the load balancer should publish on when work is available
        /// </summary>
        public string AvailableReq { get; set; } = "lb/req/" + Topic;
        /// <summary>
        /// topic the load balancer should subscribe to for receiving responses to work available
        /// </summary>
        public string AvailableResp { get; set; } = "lb/rsp/+/" + Topic;
        /// <summary>
        /// topic the load balancer should publish to to get a worker to start on the original message
        /// </summary>
        public string DoWork { get; set; } = "lb/work/" + Group + "/" + WorkerId + "/" + Topic;

        internal string GetWorkerSubAck(string group)
            => WorkerSubAck
            .Replace(Group, group);

        internal string GetAvailableReq(string topic)
            => AvailableReq
            .Replace(Topic, topic);

        internal string GetAvailableResp(string topic)
            => AvailableResp
            .Replace(Topic, topic);

        internal string GetDoWork(string group, string workerId, string topic)
            => DoWork
            .Replace(Group, group)
            .Replace(WorkerId, workerId)
            .Replace(Topic, topic);
    }
}
