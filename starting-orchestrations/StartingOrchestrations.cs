using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace starting_orchestrations
{
    public static class StartingOrchestrations
    {
        public class OrchestrationInput
        {
            public int Id { get; set; }
            public int WaitTimeInMilliseconds { get; set; }
        }

        [FunctionName("HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            TraceWriter log)
        {
            // Determine path
            var orchestrationInput = new OrchestrationInput();

            var query = req.RequestUri.ParseQueryString();
            var instanceId = query["instanceId"];
            if (string.IsNullOrEmpty(instanceId))
            {
                log.Error("*** instanceId querystring value missing");
                return req.CreateResponse(HttpStatusCode.BadRequest, "instanceId querystring value missing", new JsonMediaTypeFormatter());
            }

            var idString = query["id"];
            if (string.IsNullOrEmpty(instanceId))
            {
                log.Error("*** id querystring value missing");
                return req.CreateResponse(HttpStatusCode.BadRequest, "id querystring value missing", new JsonMediaTypeFormatter());
            }
            if (int.TryParse(idString, out int id))
            {
                orchestrationInput.Id = id;
            }
            else
            {
                log.Error("*** couldn't parse id querystring value");
                return req.CreateResponse(HttpStatusCode.BadRequest, "couldn't parse id querystring value", new JsonMediaTypeFormatter());
            }

            var waitTimeInMillisecondsString = query["wait"];
            if (string.IsNullOrEmpty(instanceId))
            {
                log.Error("*** wait querystring value missing");
                return req.CreateResponse(HttpStatusCode.BadRequest, "wait querystring value missing", new JsonMediaTypeFormatter());
            }
            if (int.TryParse(waitTimeInMillisecondsString, out int waitTimeInMilliseconds))
            {
                orchestrationInput.WaitTimeInMilliseconds = waitTimeInMilliseconds;
            }
            else
            {
                log.Error("*** couldn't parse wait querystring value");
                return req.CreateResponse(HttpStatusCode.BadRequest, "couldn't parse wait querystring value", new JsonMediaTypeFormatter());
            }


            // Find or start an orchestration instance
            log.Info($"*** TRIGGER {instanceId}, {id}: Looking up instance");
            var status = await starter.GetStatusAsync(instanceId);
            if (status == null)
            {
                log.Info($"*** TRIGGER {instanceId}, {id}: no instance found - starting...");
                await starter.StartNewAsync("StorageBatches", instanceId, orchestrationInput);
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
            var orchestrationInput = context.GetInput<OrchestrationInput>();
            if (!context.IsReplaying)
            {
                log.Info($"*** ORCHESTRATOR Starting: {context.InstanceId} - id = {orchestrationInput.Id}, waitMs = {orchestrationInput.WaitTimeInMilliseconds}");
            }

            if (orchestrationInput.WaitTimeInMilliseconds > 0)
            {
                var fireAt = context.CurrentUtcDateTime + TimeSpan.FromMilliseconds(orchestrationInput.WaitTimeInMilliseconds);
                context.CreateTimer(fireAt, CancellationToken.None);
            }

            log.Info($"*** ORCHESTRATOR Done: {context.InstanceId} - id = {orchestrationInput.Id}");

            return true;
        }

    }
}
