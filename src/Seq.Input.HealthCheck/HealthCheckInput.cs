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
            HelpText = "The HTTP or HTTPS URL that the health check will periodically GET. Multiple URLs " +
                       "can be checked; enter one per line.",
            InputType = SettingInputType.LongText)]
        public string TargetUrl { get; set; }

        [SeqAppSetting(InputType = SettingInputType.Password, IsOptional = true, DisplayName = "Authentication Header",
            HelpText = "An optional `Name: Value` header, stored as sensitive data, for authentication purposes.")]
        public string AuthenticationHeader { get; set; }

        [SeqAppSetting(
            DisplayName = "Interval (seconds)",
            IsOptional = true,
            HelpText = "The time between checks; the default is 60.")]
        public int IntervalSeconds { get; set; } = 60;

        [SeqAppSetting(
            DisplayName = "Data extraction expression",
            IsOptional = true,
            HelpText = "A Seq query language expression used to extract information from JSON responses. " +
                       "The expression will be evaluated against the response to produce a `Data` property" +
                       " on the resulting event. Use the special value `@Properties` to capture the whole " +
                       "response. The response must be UTF-8 `application/json` for this to be applied.")]
        public string DataExtractionExpression { get; set; }

        [SeqAppSetting(
            DisplayName = "Bypass HTTP caching",
            IsOptional = true,
            HelpText = "If selected, the unique probe id will be appended to the target URL query string as " +
                       "`" + HttpHealthCheck.ProbeIdParameterName  + "`, in order to disable any " +
                       "intermediary HTTP caching. The `Cache-Control: no-store` header will also be sent.")]
        public bool BypassHttpCaching { get; set; }

        public void Start(TextWriter inputWriter)
        {
            _httpClient = HttpHealthCheckClient.Create();
            var reporter = new HealthCheckReporter(inputWriter);

            JsonDataExtractor extractor = null;
            if (!string.IsNullOrWhiteSpace(DataExtractionExpression))
                extractor = new JsonDataExtractor(DataExtractionExpression);

            var targetUrls = TargetUrl.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var targetUrl in targetUrls)
            {
                var healthCheck = new HttpHealthCheck(
                    _httpClient,
                    App.Title,
                    targetUrl,
                    AuthenticationHeader,
                    extractor,
                    BypassHttpCaching);

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
