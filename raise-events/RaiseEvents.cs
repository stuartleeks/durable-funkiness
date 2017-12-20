using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace raise_events
{
    public static class RaiseEvents
    {
        [FunctionName("RaiseEvents")]
        public static async Task<bool> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {

            int i = 0;
            while (i < 10)
            {
                await context.CallActivityAsync<string>("RaiseEvents_Hello", i);
                await context.WaitForExternalEvent<object>("TheEvent");
            }

            return true;
        }

        [FunctionName("RaiseEvents_Hello")]
        public static void SayHello([ActivityTrigger] int index, TraceWriter log)
        {
            log.Info($"Saying hello: {index}.");
        }

        [FunctionName("RaiseEvents_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Start")]
            HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            TraceWriter log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("RaiseEvents", null);

            log.Info($"Started orchestration with ID = '{instanceId}'.");

            // workaround for https://github.com/Azure/azure-functions-durable-extension/issues/101
            log.Info($"Checking for orchestration with ID {instanceId}...");
            while (null == await starter.GetStatusAsync(instanceId))
            {
                System.Threading.Thread.Sleep(500);
                log.Info($"Checking for orchestration with ID {instanceId}...");
            }
            log.Info($"Checking for orchestration with ID {instanceId}... found it!");


            await starter.RaiseEventAsync(instanceId, "TheEvent", null);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("RaiseEvents_RaiseEvent")]
        public static async Task<HttpResponseMessage> RaiseAnEvent(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "Event")]
            HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            TraceWriter log)
        {
            // Determine path
            var query = req.RequestUri.ParseQueryString();
            var instanceId = query["instanceid"];
            if (string.IsNullOrEmpty(instanceId))
            {
                log.Error("*** instanceId querystring value missing");
                return req.CreateResponse(HttpStatusCode.BadRequest, "instanceId querystring value missing", new JsonMediaTypeFormatter());
            }

            log.Info($"RaiseEvent: {instanceId}");

            var status = await starter.GetStatusAsync(instanceId);
            if (status == null)
            {
                log.Error($"*** Instance {instanceId} not found");
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Instance {instanceId} not found", new JsonMediaTypeFormatter());
            }

            await starter.RaiseEventAsync(instanceId, "TheEvent", null);


            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}