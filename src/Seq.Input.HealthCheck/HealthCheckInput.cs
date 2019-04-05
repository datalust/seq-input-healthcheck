using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Seq.Apps;

namespace Seq.Input.HealthCheck
{
    [SeqApp("Health Check Input",
        Description = "Periodically GET an HTTP resource and publish response metrics to Seq.")]
    public class HealthCheckInput : SeqApp, IPublishJson, IDisposable
    {
        readonly List<HealthCheckTask> _healthCheckTasks = new List<HealthCheckTask>();
        HttpClient _httpClient;

        [SeqAppSetting(
            DisplayName = "Target URLs",
            HelpText = "The HTTP or HTTPS URL that the health check will periodically GET. Multiple URLs can be checked; enter one per line.",
            InputType = SettingInputType.LongText)]
        public string TargetUrl { get; set; }

        [SeqAppSetting(
            DisplayName = "Interval (seconds)",
            IsOptional = true,
            HelpText = "The time between checks; the default is 60.")]
        public int IntervalSeconds { get; set; } = 60;

        public void Start(TextWriter inputWriter)
        {
            _httpClient = HttpHealthCheckClient.Create();
            var reporter = new HealthCheckReporter(inputWriter);

            var targetUrls = TargetUrl.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var targetUrl in targetUrls)
            {
                var healthCheck = new HttpHealthCheck(_httpClient, App.Title, targetUrl);
                _healthCheckTasks.Add(new HealthCheckTask(
                    healthCheck,
                    TimeSpan.FromSeconds(IntervalSeconds),
                    reporter,
                    Log));
            }
        }

        public void Stop()
        {
            foreach (var task in _healthCheckTasks)
                task.Stop();
        }

        public void Dispose()
        {
            foreach (var task in _healthCheckTasks)
                task.Dispose();

            _httpClient?.Dispose();
        }
    }
}
