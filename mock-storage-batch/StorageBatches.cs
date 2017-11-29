using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace mock_storage_batch
{
    public static class StorageBatches
    {
        [FunctionName("StorageBatches")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>("StorageBatches_Hello", "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>("StorageBatches_Hello", "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>("StorageBatches_Hello", "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("StorageBatches_Hello")]
        public static string SayHello([ActivityTrigger] string name, TraceWriter log)
        {
            log.Info($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("StorageBatches_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            TraceWriter log)
        {
            var query = req.RequestUri.ParseQueryString();
            var path = query["path"];
            if (string.IsNullOrEmpty(path))
            {
                log.Error("path querystring value missing");
                return req.CreateResponse(HttpStatusCode.BadRequest, "path querystring value missing", new JsonMediaTypeFormatter());
            }

            var folderName = Path.GetDirectoryName(path);

            var instanceId = $"instance-{folderName}";
            await starter.StartNewAsync("StorageBatches", instanceId, null);

            log.Info($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}