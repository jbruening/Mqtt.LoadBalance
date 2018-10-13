namespace Mqtt.LoadBalance.Worker
{
    public class GroupTopic
    {
        public string Group { get; set; }
        public string Topic { get; set; }

        public bool Dynamic { get; set; }
    }
}
