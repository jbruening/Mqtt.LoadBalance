namespace Mqtt.LoadBalance.Worker
{
    public class Paths
    {
        public const string Group = "{grp}";
        public const string Topic = "{top}";
        public const string Uuid = "{uuid}";
        public const string MessageTopic = "{mtop}";
        public const string WorkerId = "{wid}";

        /// <summary>
        /// topic the worker should publish to for dynamically adding new topics/groups
        /// </summary>
        public string WorkerSubs { get; set; } = "lb/sub/" + Group;
        /// <summary>
        /// topic the worker should subscribe to to see if the load balancer is responding
        /// </summary>
        public string WorkerSubAck { get; set; } = "lb/suback/+";

        /// <summary>
        /// topic that the worker should subscribe to when work is available
        /// </summary>
        public string AvailableReq { get; set; } = "lb/req/+/" + Topic;
        /// <summary>
        /// topic the worker should publish to when it can do requested work
        /// </summary>
        public string AvailableResp { get; set; } = "lb/rsp/" + Group + "/" + WorkerId + "/" + Uuid + "/" + MessageTopic;
        /// <summary>
        /// topic the worker should subscribe to to work on an original message
        /// </summary>
        public string DoWork { get; set; } = "lb/work/" + Group + "/" + WorkerId + "/" + Topic;

        /// <summary>
        /// topic the worker should subscribe to to stop waiting for work
        /// </summary>
        public string DontWork { get; set; } = "lb/dwork/+/" + Group + "/+";

        internal string GetWorkerSubs(string group)
            => WorkerSubs
            .Replace(Group, group);

        internal string GetAvailableReq(string topic)
            => AvailableReq
            .Replace(Topic, topic);

        internal string GetRequestTopic(string uuid, string msgTopic)
        {
            var pre = AvailableReq.Replace("+", uuid).Replace(Topic, "");
            return msgTopic.Substring(pre.Length);
        }

        internal string GetAvailableResp(string group, string workerId, string uuid, string msgTopic)
            => AvailableResp
            .Replace(Group, group)
            .Replace(WorkerId, workerId)
            .Replace(Uuid, uuid)
            .Replace(MessageTopic, msgTopic);

        internal string GetDoWork(string group, string workerId, string topic)
            => DoWork
            .Replace(Group, group)
            .Replace(WorkerId, workerId)
            .Replace(Topic, topic);

        internal string GetWorkTopic(string group, string workerId, string msgTopic)
        {
            var pre = DoWork.Replace(Group, group).Replace(WorkerId, workerId).Replace(Topic, "");
            return msgTopic.Substring(pre.Length);
        }

        internal string GetDontWork(string group)
            => DontWork.Replace(Group, group);
    }
}
