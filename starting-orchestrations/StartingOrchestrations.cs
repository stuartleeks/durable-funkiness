using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;

namespace starting_orchestrations
{
    public static class StartingOrchestrations
    {
        [FunctionName("HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            TraceWriter log)
        {
            // Determine path
            var query = req.RequestUri.ParseQueryString();
            var instanceId = query["instanceId"];
            if (string.IsNullOrEmpty(instanceId))
            {
                log.Error("*** instanceId querystring value missing");
                return req.CreateResponse(HttpStatusCode.BadRequest, "instanceId querystring value missing", new JsonMediaTypeFormatter());
            }


            // Find or start an orchestration instance
            log.Info($"*** TRIGGER: Looking up instance: {instanceId}");
            var status = await starter.GetStatusAsync(instanceId);
            if (status == null)
            {
                log.Info($"*** TRIGGER: no instance found - {instanceId} - starting...");
                await starter.StartNewAsync("StorageBatches", instanceId, null);
                log.Info($"*** TRIGGER: Started orchestration with ID = '{instanceId}'.");
            }
            else
            {
                log.Info($"*** TRIGGER: Got existing instance for {instanceId} (name {status.Name}). status {status.RuntimeStatus})");
            }

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("StorageBatches")]
        public static bool RunOrchestrator(
          [OrchestrationTrigger] DurableOrchestrationContext context,
          TraceWriter log)
        {
            log.Info($"*** ORCHESTRATOR Starting: {context.InstanceId}");
            return true;
        }
    }
}
