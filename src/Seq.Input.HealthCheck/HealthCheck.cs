using System;

namespace Seq.Input.HealthCheck
{
    class HealthCheck : IDisposable
    {
        readonly string _targetUrl;
        readonly TimeSpan _timeSpan;
        readonly HealthCheckReporter _reporter;

        public HealthCheck(string targetUrl, TimeSpan timeSpan, HealthCheckReporter reporter)
        {
            _targetUrl = targetUrl ?? throw new ArgumentNullException(nameof(targetUrl));
            _timeSpan = timeSpan;
            _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        }

        public void Stop()
        {

        }

        public void Dispose()
        {
            
        }
    }
}