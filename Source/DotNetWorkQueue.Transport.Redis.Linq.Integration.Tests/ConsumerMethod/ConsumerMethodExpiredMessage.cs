﻿using System;
using System.Threading;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("Consumer")]
    public class ConsumerMethodExpiredMessage
    {
        [Theory]
#if NETFULL
         [InlineData(100, 0, 20, 5, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic)]
#else
        [InlineData(100, 0, 20, 5, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled)]
#endif
        public void Run(int messageCount, int runtime, 
            int timeOut, int workerCount, ConnectionInfoTypes type, LinqMethodTypes linqMethodTypes)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (
                var queueCreator =
                    new QueueCreationContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {
                    var id = Guid.NewGuid();
                    var producer = new ProducerMethodShared();
                    if (linqMethodTypes == LinqMethodTypes.Compiled)
                    {
                        producer.RunTestCompiled<RedisQueueInit>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateExpiredData,
                            Helpers.Verify, false, id, GenerateMethod.CreateCompiled, runtime, null, false);
                    }
#if NETFULL
                    else
                    {
                        producer.RunTestDynamic<RedisQueueInit>(queueName,
                           connectionString, false, messageCount, logProvider, Helpers.GenerateExpiredData,
                           Helpers.Verify, false, id, GenerateMethod.CreateDynamic, runtime, null, false);
                    }
#endif
                    Thread.Sleep(2000);

                    var consumer = new ConsumerMethodExpiredMessageShared();
                    consumer.RunConsumer<RedisQueueInit>(queueName, connectionString, false,
                        logProvider,
                        runtime, messageCount,
                        workerCount, timeOut, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%10)", id, false);

                    using (var count = new VerifyQueueRecordCount(queueName, connectionString))
                    {
                        count.Verify(0, false, -1);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<RedisQueueCreation>(queueName,
                                connectionString)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }
    }
}
