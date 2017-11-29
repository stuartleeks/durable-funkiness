using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        // TODO - morph the implementation towards this definition:
        //public class BatchContext
        //{
        //    public string CustomerId { get; set; }
        //    public string BatchId { get; set; }
        //    public string[] RequiredFiles { get; set; }
        //}
        public class BatchContext
        {
            public string FolderName { get; set; }
            public string[] RequiredFiles { get; set; }
        }

        [FunctionName("StorageBatches")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            TraceWriter log)
        {
            var folderName = context.GetInput<string>();
            log.Info($"Starting: {folderName}");

            var filesToWaitFor = new[] { "file1.txt", "file2.txt", "file3.txt", "file4.txt" };

            var batchContext = new BatchContext
            {
                FolderName = folderName,
                RequiredFiles = filesToWaitFor
            };

            var fileWaitTasks = filesToWaitFor.Select(f => context.WaitForExternalEvent<object>(f));
            await Task.WhenAll(fileWaitTasks);

            // Currently process all files in a single activity function
            // If each file can be processed independently then could split into multiple activity invocations
            await context.CallActivityAsync("ProcessFiles", batchContext);

            log.Info($"Done: {folderName}");
            return folderName;
        }

        [FunctionName("ProcessFiles")]
        public static void ProcessFiles(
            [ActivityTrigger]
            BatchContext batchContext,
            TraceWriter log)
        {
            foreach (var filename in batchContext.RequiredFiles)
            {
                // TODO - insert real processing here (for now, just deleting)
                var path = Path.Combine(batchContext.FolderName, filename);
                if (File.Exists(path))
                {
                    log.Verbose($"Deleting {path}");
                    File.Delete(path);
                }
                else
                {
                    log.Error($"Missing file {path}"); // shouldn't hit this!
                }
            }
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

            await RaiseEvent(starter, log, folderName, instanceId, "file1.txt");
            await RaiseEvent(starter, log, folderName, instanceId, "file2.txt");
            await RaiseEvent(starter, log, folderName, instanceId, "file3.txt");
            await RaiseEvent(starter, log, folderName, instanceId, "file4.txt");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        private static async Task RaiseEvent(DurableOrchestrationClient starter, TraceWriter log, string folderName, string instanceId, string name)
        {
            if (File.Exists(Path.Combine(folderName, $"{name}")))
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