using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BashAPI
{
    public class MyHealthCheck : IHealthCheck
    {
        /*
        public MyHealthCheck(int i, string s)
        {
            I = i;
            S = s;
        }

        public int I { get; set; }

        public string S { get; set; }
        */
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var healthCheckResultHealthy = true;

            if (healthCheckResultHealthy)
            {
                return Task.FromResult(
                    HealthCheckResult.Healthy("A healthy result."));
            }

            return Task.FromResult(
                new HealthCheckResult(context.Registration.FailureStatus,
                "An unhealthy result."));
        }
    }
}
