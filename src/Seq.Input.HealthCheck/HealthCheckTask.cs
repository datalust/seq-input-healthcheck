// Copyright 2019 Datalust and contributors. 
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Seq.Input.HealthCheck
{
    class HealthCheckTask : IDisposable
    {
        readonly CancellationTokenSource _cancel = new CancellationTokenSource();
        readonly Task _healthCheckTask;

        public HealthCheckTask(HttpHealthCheck healthCheck, TimeSpan interval, HealthCheckReporter reporter, ILogger diagnosticLog)
        {
            if (healthCheck == null) throw new ArgumentNullException(nameof(healthCheck));
            if (reporter == null) throw new ArgumentNullException(nameof(reporter));

            _healthCheckTask = Task.Run(() => Run(healthCheck, interval, reporter, diagnosticLog, _cancel.Token), _cancel.Token);
        }

        static async Task Run(
            HttpHealthCheck healthCheck,
            TimeSpan interval, 
            HealthCheckReporter reporter, 
            ILogger diagnosticLog,
            CancellationToken cancel)
        {
            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    var result = await healthCheck.CheckNow(cancel);
                    reporter.Report(result);

                    if (result.Elapsed < interval.TotalMilliseconds)
                    {
                        var delay = (int) (interval.TotalMilliseconds - result.Elapsed);
                        await Task.Delay(delay, cancel);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Unloading
            }
            catch (Exception ex)
            {
                diagnosticLog.Fatal(ex, "The health check task threw an unhandled exception");
            }
        }

        public void Stop()
        {
            _cancel.Cancel();
            _healthCheckTask.Wait();
        }

        public void Dispose()
        {
            _cancel.Dispose();
            _healthCheckTask.Dispose();
        }
    }
}