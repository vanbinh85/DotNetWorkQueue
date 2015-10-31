﻿// ---------------------------------------------------------------------
// Copyright (c) 2015 Brian Lehnen
// 
// All rights reserved.
// 
// MIT License
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleShared;
using DotNetWorkQueue;
using DotNetWorkQueue.Logging;
using ExampleMessage;
using Serilog;

namespace ConsoleSharedCommands.Commands
{
    public abstract class SharedConsumeMessage<TTransportInit> : SharedCommands
        where TTransportInit: class, ITransportInit, new()
    {
        private readonly Lazy<QueueContainer<TTransportInit>> _queueContainer;
        protected readonly Dictionary<string, IConsumerQueue> Queues;

        protected SharedConsumeMessage()
        {
            _queueContainer = new Lazy<QueueContainer<TTransportInit>>(CreateContainer);
            Queues = new Dictionary<string, IConsumerQueue>();
        }

        public override ConsoleExecuteResult Example(string command)
        {
            switch (command)
            {
                case "SetWorkerConfiguration":
                    return new ConsoleExecuteResult("SetWorkerConfiguration examplequeue 5 true false 00:00:05 00:00:5");
                case "SetHeartBeatConfiguration":
                    return new ConsoleExecuteResult("SetHeartBeatConfiguration examplequeue 2 00:00:30 00:00:10 1 1 00:01:00");
                case "SetMessageExpirationConfiguration":
                    return new ConsoleExecuteResult("SetMessageExpirationConfiguration examplequeue true 00:01:00");
                case "SetFatalExceptionDelayBehavior":
                    return new ConsoleExecuteResult("SetFatalExceptionDelayBehavior examplequeue 00:00:01,00:00:05,00:00:30");
                case "SetQueueDelayBehavior":
                    return new ConsoleExecuteResult("SetQueueDelayBehavior examplequeue 00:00:01,00:00:02,00:00:03");
                case "SetQueueRetryBehavior":
                    return new ConsoleExecuteResult("SetQueueRetryBehavior examplequeue System.TimeoutException 00:00:01,00:00:02,00:00:03");

                case "StartQueue":
                    return new ConsoleExecuteResult("StartQueue examplequeue");
                case "StopQueue":
                    return new ConsoleExecuteResult("StopQueue examplequeue");
            }
            return base.Example(command);
        }

        public ConsoleExecuteResult EnableSerilog()
        {
            var log = new LoggerConfiguration()
                .WriteTo.ColoredConsole(outputTemplate: "{Timestamp:HH:mm} [{Level}] ({Name:l}) {Message}{NewLine}{Exception}")
                .CreateLogger();
            Log.Logger = log;

            return
                new ConsoleExecuteResult(
                    "Serilog enabled; however, this will only take affect if the main library was compiled in release mode, as debug mode uses console logging unless another provider was injected");
        }

        public override ConsoleExecuteResult Help()
        {
            var help = new StringBuilder();
            help.AppendLine(base.Help().Message);
            help.AppendLine(ConsoleFormatting.FixedLength("EnableSerilog",
                "Uses serilog for logging to console; note that this example only works when the worker library is compiled in release mode; this is an example limitation, not a library limitation"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetWorkerConfiguration queueName",
                "Worker configuration options"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetHeartBeatConfiguration queueName",
                "HeartBeat configuration options"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetMessageExpirationConfiguration queueName",
                "Message Expiration configuration options"));

            help.AppendLine(ConsoleFormatting.FixedLength("SetFatalExceptionDelayBehavior queueName",
                "Back off times for when fatal errors occur"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetQueueDelayBehavior queueName",
                "Back off times for when the queue is empty"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetQueueRetryBehavior queueName",
                "Retry strategy, based on the type of the exception"));

            help.AppendLine(ConsoleFormatting.FixedLength("StartQueue queueName", "Starts a queue"));
            help.AppendLine(ConsoleFormatting.FixedLength("StopQueue queueName",
                "Stops a queue; configuration will be reset"));
            return new ConsoleExecuteResult(help.ToString());
        }

        public ConsoleExecuteResult SetFatalExceptionDelayBehavior(string queueName, params TimeSpan[] timespans)
        {
            CreateModuleIfNeeded(queueName);
            Queues[queueName].Configuration.TransportConfiguration.FatalExceptionDelayBehavior.Clear();
            Queues[queueName].Configuration.TransportConfiguration.FatalExceptionDelayBehavior.Add(timespans.ToList());
            return new ConsoleExecuteResult("fatal exception delays have been set");
        }

        public ConsoleExecuteResult SetQueueDelayBehavior(string queueName, params TimeSpan[] timespans)
        {
            CreateModuleIfNeeded(queueName);
            Queues[queueName].Configuration.TransportConfiguration.QueueDelayBehavior.Clear();
            Queues[queueName].Configuration.TransportConfiguration.QueueDelayBehavior.Add(timespans.ToList());
            return new ConsoleExecuteResult("queue delays have been set");
        }

        public ConsoleExecuteResult SetQueueRetryBehavior(string queueName, string exceptionType, params TimeSpan[] timespans)
        {
            CreateModuleIfNeeded(queueName);
            Queues[queueName].Configuration.TransportConfiguration.RetryDelayBehavior.Add(Type.GetType(exceptionType, true), timespans.ToList());
            return new ConsoleExecuteResult("queue delays have been set");
        }

        public ConsoleExecuteResult SetWorkerConfiguration(string queueName, 
            int workerCount = 1,
            bool singleWorkerWhenNoWorkFound = true,
            bool abortWorkerThreadsWhenStopping = false,
            TimeSpan? timeToWaitForWorkersToStop = null,
            TimeSpan? timeToWaitForWorkersToCancel = null
            )
        {
            CreateModuleIfNeeded(queueName);
            Queues[queueName].Configuration.Worker.WorkerCount = workerCount;
            Queues[queueName].Configuration.Worker.SingleWorkerWhenNoWorkFound = singleWorkerWhenNoWorkFound;
            Queues[queueName].Configuration.Worker.AbortWorkerThreadsWhenStopping = abortWorkerThreadsWhenStopping;
            if (timeToWaitForWorkersToCancel.HasValue)
            {
                Queues[queueName].Configuration.Worker.TimeToWaitForWorkersToCancel =
                    timeToWaitForWorkersToCancel.Value;
            }
            if (timeToWaitForWorkersToStop.HasValue)
            {
                Queues[queueName].Configuration.Worker.TimeToWaitForWorkersToStop =
                    timeToWaitForWorkersToStop.Value;
            }

            return new ConsoleExecuteResult($"worker configuration set for {queueName}");
        }

        public ConsoleExecuteResult SetHeartBeatConfiguration(string queueName, 
            int interval = 2, 
            TimeSpan? monitorTime = null,
            TimeSpan? deadTime = null,
            int heartbeatThreadsMax = 1,
            int heartbeatThreadsMin = 1,
            TimeSpan? threadIdle = null
            )
        {
            CreateModuleIfNeeded(queueName);
            Queues[queueName].Configuration.HeartBeat.Interval = interval;
            if (deadTime.HasValue)
            {
                Queues[queueName].Configuration.HeartBeat.Time = deadTime.Value;
            }
            if (monitorTime.HasValue)
            {
                Queues[queueName].Configuration.HeartBeat.MonitorTime = monitorTime.Value;
            }
            Queues[queueName].Configuration.HeartBeat.ThreadPoolConfiguration.ThreadsMax = heartbeatThreadsMax;
            Queues[queueName].Configuration.HeartBeat.ThreadPoolConfiguration.ThreadsMin = heartbeatThreadsMin;
            if (threadIdle.HasValue)
            {
                Queues[queueName].Configuration.HeartBeat.ThreadPoolConfiguration.ThreadIdleTimeout = threadIdle.Value;
            }

            return new ConsoleExecuteResult($"heartbeat configuration set for {queueName}");
        }

        public ConsoleExecuteResult SetMessageExpirationConfiguration(string queueName,
            bool enabled = true,
            TimeSpan? monitorTime = null)
        {
            CreateModuleIfNeeded(queueName);
            Queues[queueName].Configuration.MessageExpiration.Enabled = enabled;
            if (monitorTime.HasValue)
            {
                Queues[queueName].Configuration.MessageExpiration.MonitorTime = monitorTime.Value;
            }
            return new ConsoleExecuteResult($"message expiration configuration set for {queueName}");
        }

        public ConsoleExecuteResult StopQueue(string queueName)
        {
            if (!Queues.ContainsKey(queueName)) return new ConsoleExecuteResult($"{queueName} was not found");
            Queues[queueName].Dispose();
            Queues.Remove(queueName);
            return new ConsoleExecuteResult($"{queueName} has been stopped");
        }

        public ConsoleExecuteResult StartQueue(string queueName)
        {
            CreateModuleIfNeeded(queueName);
            Queues[queueName].Start<SimpleMessage>((HandleMessages));
            return new ConsoleExecuteResult($"{queueName} started");
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var queue in Queues.Values)
            {
                queue.Dispose();
            }
            Queues.Clear();
            if (_queueContainer.IsValueCreated)
            {
                _queueContainer.Value.Dispose();
            }
            base.Dispose(disposing);
        }

        protected QueueContainer<TTransportInit> CreateContainer()
        {
            return new QueueContainer<TTransportInit>(RegisterService);
        }

        protected void RegisterService(IContainer container)
        {
            if (Metrics != null)
            {
                container.Register<IMetrics>(() => Metrics, LifeStyles.Singleton);
            }

            if (Des)
            {
                container.Register(() => DesConfiguration,
                     LifeStyles.Singleton);
            }
        }

        protected void CreateModuleIfNeeded(string queueName)
        {
            if (Queues.ContainsKey(queueName)) return;

            Queues.Add(queueName,
                _queueContainer.Value.CreateConsumer(queueName,
                    ConfigurationManager.AppSettings["Connection"]));

            QueueStatus?.AddStatusProvider(
                QueueStatusContainer.Value.CreateStatusProvider<TTransportInit>(queueName,
                    ConfigurationManager.AppSettings["Connection"]));
        }

        private void HandleMessages(IReceivedMessage<SimpleMessage> message, IWorkerNotification notifications)
        {
            notifications.Log.Debug(
                $"Processing Message {message.MessageId} with run time {message.Body.RunTimeInMs}");

            if (message.Body.RunTimeInMs > 0)
            {
                var end = DateTime.Now + TimeSpan.FromMilliseconds(message.Body.RunTimeInMs);
                if (notifications.TransportSupportsRollback)
                {
                    Task.Delay(message.Body.RunTimeInMs, notifications.WorkerStopping.CancelWorkToken).Wait(notifications.WorkerStopping.CancelWorkToken);
                }
                else //no rollback possible; we will ignore cancel / stop requests
                {
                    Task.Delay(message.Body.RunTimeInMs);
                }

                if (DateTime.Now < end) //did we finish?
                { //nope - we probably are being canceled
                    if (notifications.TransportSupportsRollback && notifications.WorkerStopping.CancelWorkToken.IsCancellationRequested)
                    {
                        notifications.Log.Debug("Cancel has been requested - aborting");
                        notifications.WorkerStopping.CancelWorkToken.ThrowIfCancellationRequested();
                    }
                }
            }
            notifications.Log.Debug($"Processed message {message.MessageId}");
        }
    }
}