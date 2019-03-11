using System;
using Newtonsoft.Json;

namespace Seq.Input.HealthCheck
{
    class HealthCheckResult
    {
        [JsonProperty("@t")]
        public DateTime UtcTimestamp { get; }

        [JsonProperty("@x", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Exception Exception { get; }

        [JsonProperty("@mt")]
        public string MessageTemplate { get; } =
            "Health check {Method} {TargetUrl} {Outcome} with status code {StatusCode} in {Elapsed:0.000} ms";

        [JsonProperty("@l", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Level { get; }

        public string HealthCheckTitle { get; }
        public string Method { get; }
        public string TargetUrl { get; }
        public string Outcome { get; }
        public int? Elapsed { get; }
        public int? StatusCode { get; }
        public string ContentType { get; }
        public int? ContentLength { get; }
        public string InitialContent { get; }

        public HealthCheckResult(
            DateTime utcTimestamp,
            string healthCheckTitle,
            string method,
            string targetUrl,            
            string outcome,
            string level,
            int? elapsed,
            int? statusCode,
            string contentType,
            int? contentLength,
            string initialContent,
            Exception exception)
        {
            if (utcTimestamp.Kind != DateTimeKind.Utc)
                throw new ArgumentException("The timestamp must be UTC.", nameof(utcTimestamp));
 
            UtcTimestamp = utcTimestamp;

            HealthCheckTitle = healthCheckTitle ?? throw new ArgumentNullException(nameof(healthCheckTitle));
            Method = method ?? throw new ArgumentNullException(nameof(method));
            TargetUrl = targetUrl ?? throw new ArgumentNullException(nameof(targetUrl));
            Outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));

            Level = level;
            Elapsed = elapsed;
            StatusCode = statusCode;
            ContentType = contentType;
            ContentLength = contentLength;
            InitialContent = initialContent;
            Exception = exception;
        }
    }
}
