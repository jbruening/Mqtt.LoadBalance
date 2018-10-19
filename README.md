# Mqtt.LoadBalance
mqtt client libraries to perform load balanced work (when the workers are actually available to perform work), without needing a custom broker

## terms
```{wid}``` a unique identifier for a worker in a group.  
```{group}``` name for a group of workers that have work distributed among them  
```{topic}``` the original mqtt topic to subscribe to  
```{mtopic}``` the original topic messages are published over  
```(message)``` the original message published over ```{mtopic}```  
```{uuid}``` a [uuid](https://en.wikipedia.org/wiki/Universally_unique_identifier)

## sequence of operations 

### load balancer subscribing to necessary topic paths for group
can be done during load balancer startup  
load balancer remembers group if not done so  
load balancer subs to ```{topic}``` if not done so  
load balancer subs to ```lb/rsp/+/+/{topic}``` if not done so  

### dynamic group/topic load balancing
load balancer subs to ```lb/sub/+```   
workers pub to ```lb/sub/{group}``` with ```{topic}``` as payload, occasionally as a 'load balancer keepalive'  
load balancer receives pub  
load balancer subscribes to necessary topic paths  
load balancer pubs to ```lb/suback/{group}``` with ```{topic}``` as payload  
workers notice that load balancer is available
workers subscribe to paths based on their load balancing type  

### original message published. 
```(message)``` comes through ```{mtopic}```  
#### Agent-based adaptive load balancing
workers subscribe to ```lb/req/+/{topic}```  
workers subscribe to ```lb/work/{group}/{wid}/{topic}```  
load balancer pub ```lb/req/{uuid}/{mtopic}```  
workers respond to ```lb/rsp/{group}/{wid}/{uuid}/{mtopic}``` if they can do the work  
the load balancer then pubs the ```(message)``` to ```lb/work/{group}/{wid}/{mtopic}``` to all first responding workers in each group  

### todo: other load balancing schemes

## other todos:
if the client doesn't see suback, assume no load balancer and subscribe to ```{topic}```. if it does later, unsubscribe from ```{topic}```  
