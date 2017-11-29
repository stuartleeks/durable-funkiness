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

        public static class KnownRuntimeStatuses
        {
            public const string Running = "Running";
            public const string Completed = "Completed";
        }
        public static class EventNames
        {
            // one or more new files to inspect
            public const string NewFiles = "NewFiles";
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

            // Wait for all required files
            // Pushing IO to activity function as per https://docs.microsoft.com/en-us/azure/azure-functions/durable-functions-perf-and-scale#thread-usage
            while (! await context.CallActivityAsync<bool>("AreRequiredFilesPresent", batchContext))
            {
                await context.WaitForExternalEvent<object>(EventNames.NewFiles);
            }

            // Currently process all files in a single activity function
            // If each file can be processed independently then could split into multiple activity invocations
            await context.CallActivityAsync("ProcessFiles", batchContext);

            log.Info($"Done: {folderName}");
            return folderName;
        }

        [FunctionName("AreRequiredFilesPresent")]
        public static bool AreRequiredFilesPresent(
            [ActivityTrigger]
            BatchContext batchContext,
            TraceWriter log)
        {
            // TODO  - replace this with test for blob file existence
            return batchContext.RequiredFiles
                        .Select(f => Path.Combine(batchContext.FolderName, f))
                        .All(File.Exists);
        }

        [FunctionName("ProcessFiles")]
        public static void ProcessFiles(
            [ActivityTrigger]
            BatchContext batchContext,
            TraceWriter log)
        {
            log.Info($"*** ProcessFiles {batchContext.FolderName}");
            foreach (var filename in batchContext.RequiredFiles)
            {
                // TODO - insert real processing here
                System.Threading.Thread.Sleep(2500);

                // TODO - replace local file access with blob access
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
            // TODO - replace HttpTrigger with eventgrid trigger for blobs
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
                if (status.RuntimeStatus == KnownRuntimeStatuses.Running)
                {
                    log.Info($"Got existing instance for {instanceId} (name {status.Name}). Raising external event  - status {status.RuntimeStatus})");
                    await starter.RaiseEventAsync(instanceId, EventNames.NewFiles, null);
                }
                else
                {
                    // TODO determine whether this is a case that should be filtered out (e.g. if there are surplus files that could trigger us but should be ignored)
                    log.Error($"Got existing instance for {instanceId} (name {status.Name}), but it isn't running");
                }
            }

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}