﻿using System;
using System.Collections.Generic;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Route;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Microsoft.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Microsoft.Integration.Tests.Route
{
    [Collection("SQLite")]
    public class RouteMultiTests
    {
        [Theory]
        [InlineData(10, 0, 400, 1, false, 2, true),
        InlineData(100, 0, 180, 1, true, 2, false)]
        public void Run(int messageCount, int runtime, int timeOut, int readerCount,
          bool inMemoryDb, int routeCount, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<SqLiteMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
                                   connectionInfo.ConnectionString)
                        )
                        {
                            oCreation.Options.EnableDelayedProcessing = true;
                            oCreation.Options.EnableHeartBeat = true;
                            oCreation.Options.EnableStatus = true;
                            oCreation.Options.EnableStatusTable = true;
                            oCreation.Options.EnableRoute = true;

                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            var routeTest = new RouteMultiTestsShared();
                            routeTest.RunTest<SqLiteMessageQueueInit, FakeMessageA>(queueName,
                                connectionInfo.ConnectionString,
                                true, messageCount, logProvider, Helpers.GenerateData, Helpers.Verify, false,
                                GenerateRoutes(routeCount, 1), GenerateRoutes(routeCount, routeCount + 1), runtime,
                                timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), oCreation.Scope, "second(*%3)", enableChaos);

                            new VerifyQueueRecordCount(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(0, false, false);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                        )
                        {
                            oCreation.RemoveQueue();
                        }
                    }
                }
            }
        }
        private List<string> GenerateRoutes(int routeCount, int seed)
        {
            var data = new List<string>();
            for (var i = seed; i < routeCount + seed; i++)
            {
                data.Add("Route" + i);
            }
            return data;
        }
    }
}
