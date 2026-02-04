using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VueReporting.Models;
using VueReporting.Services;
using VueReporting.Services.Errors;

namespace VueReporting
{
    public class QueueSystem
    {
        public class ItemInfo
        {
            public string Name { get; set; }
            public bool InProgress { get; set; }
            public bool Completed { get; set; }
            public bool Error { get; set; }
            public string Message { get; set; }
        }
        public class AfterQueueItem
        {
            public string Name { get; set; }
            public bool Completed { get; set; }
            public bool Error { get; set; }
            public string Message { get; set; }
        }
        public class QueueItem
        {
            public string Name { get; set; }
            public Func<Task> Generate { get; set; }
        }

        private static ILogger _logger;
        private static IScheduler _scheduler;

        private static readonly ConcurrentQueue<QueueItem> ItemsInQueue = new ConcurrentQueue<QueueItem>();
        private static readonly List<AfterQueueItem> ItemsAfterQueue = new List<AfterQueueItem>();
        public static async Task InitializeAsync(ILoggerFactory loggerFactory)
        {
            if (_scheduler != null)
            {
                throw new InvalidOperationException("Already started.");
            }

            _logger = loggerFactory.CreateLogger<QueueSystem>();

            var properties = new NameValueCollection
            {
                ["quartz.serializer.type"] = "json"
            };

            var schedulerFactory = new StdSchedulerFactory(properties);
            _scheduler = await schedulerFactory.GetScheduler();
            await _scheduler.Start();

            var reportJob = JobBuilder.Create<ReportJob>()
                .WithIdentity("ReportGeneration")
                .Build();
            var reportTrigger = TriggerBuilder.Create()
                .WithIdentity("ReportGenerationTrigger")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(10)
                    .RepeatForever())
                .Build();

            await _scheduler.ScheduleJob(reportJob, reportTrigger);
        }

        public static void AddReport(QueueItem item)
        {
            ItemsInQueue.Enqueue(item);
        }

        public static ItemInfo[] GetCurrentStatus()
        {
            var queue = ItemsInQueue.Select(x => new ItemInfo
            {
                Name = x.Name,
                Completed = false,
                InProgress = false,
                Error = false
            });
            var afterQueue = ItemsAfterQueue.Select(x => new ItemInfo
            {
                Name = x.Name,
                Completed = x.Completed,
                InProgress = !x.Completed,
                Error = x.Error,
                Message = x.Message
            });

            return afterQueue
                .Concat(queue)
                .ToArray();
        }

        [DisallowConcurrentExecution]
        private class ReportJob : IJob
        {
            public async Task Execute(IJobExecutionContext context)
            {
                if (ItemsInQueue.TryDequeue(out QueueItem result))
                {
                    var completedItem = new AfterQueueItem
                    {
                        Name = result.Name,
                        Error = false,
                        Completed = false
                    };
                    ItemsAfterQueue.Add(completedItem);

                    try
                    {
                        await result.Generate();
                        completedItem.Completed = true;
                    }
                    catch(Exception e)
                    {
                        var errorMsg = e is ReportGenerationException
                            ? "Error generation failed. Please, check the error report in output folder."
                            : "An error has occurred, please try again. If the error continues, please contact support.";
                        _logger.LogError(e, "error during generation of {1}", result.Name);
                        completedItem.Completed = true;
                        completedItem.Error = true;
                        completedItem.Message = errorMsg;
                    }
                }
            }
        }
    }
}
