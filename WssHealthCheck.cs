using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    public class WssHealthCheck : IHealthCheck
    {
        public static bool healthy = true;
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var result = healthy
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Die");

            return Task.FromResult(result);
        }
    }
}