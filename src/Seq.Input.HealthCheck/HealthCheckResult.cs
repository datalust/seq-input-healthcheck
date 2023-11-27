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
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Seq.Input.HealthCheck
{
    class HealthCheckResult
    {
        [JsonProperty("@t")]
        public DateTime UtcTimestamp { get; }

        [JsonProperty("@x", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? Exception { get; }

        [JsonProperty("@mt")]
        public string MessageTemplate { get; } =
            "Health check {Method} {TargetUrl} {Outcome} with status code {StatusCode} in {Elapsed:0.000} ms";
        
        [JsonProperty("@l", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? Level { get; }

        [JsonProperty("@r")]
        public string[] Renderings => new[] {Elapsed.ToString("0.000", CultureInfo.InvariantCulture)};

        public string HealthCheckTitle { get; }
        public string Method { get; }
        public string TargetUrl { get; }
        public string Outcome { get; }
        public double Elapsed { get; }
        public int? StatusCode { get; }
        public string? ContentType { get; }
        public long? ContentLength { get; }
        public string ProbeId { get; }
        public string? FinalUrl { get; set; }
        public int RedirectCount { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? InitialContent { get; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public JToken? Data { get; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? ProbedUrl { get; }

        public HealthCheckResult(
            DateTime utcTimestamp,
            string healthCheckTitle,
            string method,
            string targetUrl,
            string outcome,
            string probeId,
            string? level,
            double elapsed,
            int? statusCode,
            string? contentType,
            long? contentLength,
            string? initialContent,
            Exception? exception,
            JToken? data,
            string? probedUrl,
            int redirectCount,
            string? finalUrl)
        {
            if (utcTimestamp.Kind != DateTimeKind.Utc)
                throw new ArgumentException("The timestamp must be UTC.", nameof(utcTimestamp));

            UtcTimestamp = utcTimestamp;

            HealthCheckTitle = healthCheckTitle ?? throw new ArgumentNullException(nameof(healthCheckTitle));
            Method = method ?? throw new ArgumentNullException(nameof(method));
            TargetUrl = targetUrl ?? throw new ArgumentNullException(nameof(targetUrl));
            Outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            ProbeId = probeId ?? throw new ArgumentNullException(nameof(probeId));

            Level = level;
            Elapsed = elapsed;
            StatusCode = statusCode;
            ContentType = contentType;
            ContentLength = contentLength;
            InitialContent = initialContent;
            Exception = exception?.ToString();
            Data = data;
            ProbedUrl = probedUrl;
            RedirectCount = redirectCount;
            FinalUrl = finalUrl;
        }
    }
}