﻿using DotNetWorkQueue.IntegrationTests.Metrics;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    /// <summary>
    /// Extension methods for obtaining the current metrics
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Gets the current metric values
        /// </summary>
        /// <param name="data">The data.</param>
        public static MetricsData GetCurrentMetrics(this IMetrics data)
        {
            return (MetricsData)data.CollectedMetrics;
        }
        /// <summary>
        /// Gets the current metrics.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static MetricsData GetCurrentMetrics(this Metrics.Metrics data)
        {
            return (MetricsData)data.CollectedMetrics;
        }
    }
}
