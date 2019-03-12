using System;
using System.IO;
using Seq.Apps;

namespace Seq.Input.HealthCheck
{
    [SeqApp("Health Check Input",
        Description = "Periodically GET an HTTP resource and publish response metrics to Seq.")]
    public class HealthCheckInput : SeqApp, IPublishJson, IDisposable
    {
        HealthCheckTask _healthCheckTask;
        HttpHealthCheck _httpHealthCheck;

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
            _httpHealthCheck = new HttpHealthCheck(App.Title, TargetUrl);

            _healthCheckTask = new HealthCheckTask(
                _httpHealthCheck,
                TimeSpan.FromSeconds(IntervalSeconds),
                new HealthCheckReporter(inputWriter),
                Log);
        }

        public void Stop()
        {
            _healthCheckTask.Stop();
        }

        public void Dispose()
        {
            _healthCheckTask?.Dispose();
            _httpHealthCheck?.Dispose();
        }
    }
}
