using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace simple
{
    public static class HelloFunction
    {
        // 'component' function to perform work
        [FunctionName("SayHello")]
        public static string SayHello(
            [ActivityTrigger]
            string name, 

            TraceWriter log)
        {
            log.Info($"SayHello: {name}");

            return $"Hello {name}";
        }

        // 'orchestrating' function - control activities
        [FunctionName("DoHello")]
        public static async Task<List<string>> DoHello(
            [OrchestrationTrigger]
            DurableOrchestrationContext context
            
            ,TraceWriter log
            )
        {
            log.Info("DoHello: starting");
            var outputs = new List<string>();

            outputs.Add(await context.CallActivityAsync<string>("SayHello", "Stuart"));
            outputs.Add(await context.CallActivityAsync<string>("SayHello", "Ryan"));

            return outputs;
        }

        // 'initiating' function - create a new orchestration
        [FunctionName("StartHello")]
        public static async Task<HttpResponseMessage> StartHello(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]
            HttpRequestMessage  request,
            
            [OrchestrationClient]
            DurableOrchestrationClient starter,
            
            TraceWriter log
            )
        {
            log.Info($"StartHello: starting");

            //var instanceId = "customId-" + Guid.NewGuid().ToString();
            //await starter.StartNewAsync("DoHello", instanceId, null);

            var instanceId = await starter.StartNewAsync("DoHello", null);

            log.Info($"StartHello: Started instance {instanceId}");

            return starter.CreateCheckStatusResponse(request, instanceId);
        }
    }
}
