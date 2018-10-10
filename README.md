# Mqtt.LoadBalance
mqtt client libraries to perform load balanced work (when the workers are actually available to perform work), without needing a custom broker

terms:  
{wid} a unique identifier for a worker in a group.  
{group} name for a group of workers that have work distributed among them  
{topic} the original published mqtt topic  
{wtopic} = {topic}, Replace + and # with _  
(message) the original message published over {topic}

sequence of operations:  
load balancer subs to lb/sub/+/#  
workers pub to lb/sub/{group}/{topic} occasionally as a 'load balancer keepalive'  
load balancer receives pub  
load balancer subs to {topic} if not done so  
load balancer subs to lb/rsp/+/{topic} if not done so  
load balancer pubs to lb/suback/{group}/{wtopic}  

workers notice that load balancer is available  
todo: loadbalancer is missing fallback  
if the client doesn't see suback, assume no lb and subscribe to {topic}. if it does later, unsubscribe from {topic}  

workers subscribe to lb/req/{topic} and lb/work/{group}/{wid}/{topic}  

original message published:  
(message) comes through {topic}  
load balancer pub lb/req/{topic} with contents as a uuid  
workers respond to lb/rsp/{group}/{wid}/{topic} with the uuid if they are available for work  
the load balancer then pubs the (message) to lb/work/{group}/{wid}/{topic} to all first responding workers in each group  
