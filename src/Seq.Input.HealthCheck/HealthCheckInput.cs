using System;
using System.IO;
using Seq.Apps;

namespace Seq.Input.HealthCheck
{
    [SeqApp("Health Check Input",
        Description = "Periodically GET an HTTP resource and publish response metrics to Seq.")]
    public class HealthCheckInput : SeqApp, IPublishJson, IDisposable
    {
        HealthCheck _healthCheck;

        [SeqAppSetting(
            DisplayName = "Target URL",
            HelpText = "The HTTP or HTTPS URL that the health check will periodically GET.")]
        public string TargetUrl { get; set; }

        [SeqAppSetting(
            DisplayName = "Interval (seconds)",
            IsOptional = true,
            HelpText = "The interval between checks; the default is 60.")]
        public int IntervalSeconds { get; set; } = 60;

        public void Start(TextWriter inputWriter)
        {
            _healthCheck = new HealthCheck(
                TargetUrl,
                TimeSpan.FromSeconds(IntervalSeconds),
                new HealthCheckReporter(inputWriter));
        }

        public void Stop()
        {
            _healthCheck.Stop();
        }

        public void Dispose()
        {
            _healthCheck?.Dispose();
        }
    }
}
