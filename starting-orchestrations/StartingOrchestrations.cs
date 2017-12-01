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
            var id = query["id"];
            if (string.IsNullOrEmpty(instanceId))
            {
                log.Error("*** id querystring value missing");
                return req.CreateResponse(HttpStatusCode.BadRequest, "id querystring value missing", new JsonMediaTypeFormatter());
            }


            // Find or start an orchestration instance
            log.Info($"*** TRIGGER {instanceId}, {id}: Looking up instance");
            var status = await starter.GetStatusAsync(instanceId);
            if (status == null)
            {
                log.Info($"*** TRIGGER {instanceId}, {id}: no instance found - starting...");
                await starter.StartNewAsync("StorageBatches", instanceId, id);
                log.Info($"*** TRIGGER {instanceId}, {id}: Started new orchestration");
            }
            else
            {
                log.Info($"*** TRIGGER {instanceId}, {id}: Got existing instance (name {status.Name}). status {status.RuntimeStatus})");
            }

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("StorageBatches")]
        public static bool RunOrchestrator(
          [OrchestrationTrigger] DurableOrchestrationContext context,
          TraceWriter log)
        {
            var id = context.GetInput<int>();
            log.Info($"*** ORCHESTRATOR Starting: {context.InstanceId} - id = {id}");
            return true;
        }
    }
}
