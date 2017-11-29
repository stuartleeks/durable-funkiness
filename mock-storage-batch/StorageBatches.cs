using System;
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
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            TraceWriter log)
        {
            var folderName = context.GetInput<string>();
            log.Info($"Starting: {folderName}");

            log.Verbose($"** in RunOrchestrator {folderName}");
            await context.WaitForExternalEvent<object>("file1");
            log.Verbose($"** in RunOrchestrator {folderName} **** file1");
            await context.WaitForExternalEvent<object>("file2");
            log.Verbose($"** in RunOrchestrator {folderName} **** file2");
            await context.WaitForExternalEvent<object>("file3");
            log.Verbose($"** in RunOrchestrator {folderName} **** file3");
            await context.WaitForExternalEvent<object>("file4");
            log.Verbose($"** in RunOrchestrator {folderName} **** file4");



            DeleteFile(folderName, "file1.txt", log);
            DeleteFile(folderName, "file2.txt", log);
            DeleteFile(folderName, "file3.txt", log);
            DeleteFile(folderName, "file4.txt", log);

            log.Info($"Done: {folderName}");
            return folderName;
        }

        private static void DeleteFile(string folderName, string filename, TraceWriter log)
        {
            var path = Path.Combine(folderName, filename);
            if (File.Exists(path))
            {
                log.Verbose($"Deleting {path}");
                File.Delete(path);
            }
            else
            {
                log.Error($"Missing file {path}");
            }
        }

        //[FunctionName("StorageBatches_Hello")]
        //public static string SayHello([ActivityTrigger] string name, TraceWriter log)
        //{
        //    log.Info($"Saying hello to {name}.");
        //    return $"Hello {name}!";
        //}

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

            var instanceId = $"instance-{folderName.Replace(":", "").Replace("\\", "_")}";

            log.Info($"Looking up instance: {instanceId}");
            var status = await starter.GetStatusAsync(instanceId);
            if (status == null)
            {
                log.Info($"no instance found - {instanceId} - starting...");
                await starter.StartNewAsync("StorageBatches", instanceId, folderName);
                log.Info($"Started orchestration with ID = '{instanceId}'.");
                System.Threading.Thread.Sleep(5000); // TODO - investigate the error that occurs if we remove this
            }
            else
            {
                log.Info($"Got existing instance for {instanceId} (name {status.Name} - status {status.RuntimeStatus})");
            }

            await RaiseEvent(starter, log, folderName, instanceId, "file1");
            await RaiseEvent(starter, log, folderName, instanceId, "file2");
            await RaiseEvent(starter, log, folderName, instanceId, "file3");
            await RaiseEvent(starter, log, folderName, instanceId, "file4");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        private static async Task RaiseEvent(DurableOrchestrationClient starter, TraceWriter log, string folderName, string instanceId, string name)
        {
            if (File.Exists(Path.Combine(folderName, $"{name}.txt")))
            {
                log.Info($"*** file {name} for folder {folderName} - found");
                await starter.RaiseEventAsync(instanceId, name, null);
            }
            else
            {
                log.Info($"*** file {name} for folder {folderName} - missing");
            }
        }
    }
}