using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Seq.Input.HealthCheck
{
    class HealthCheckResult
    {
        [JsonProperty("@t")]
        public DateTime UtcTimestamp { get; }

        [JsonProperty("@x", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Exception { get; }

        [JsonProperty("@mt")]
        public string MessageTemplate { get; } =
            "Health check {Method} {TargetUrl} {Outcome} with status code {StatusCode} in {Elapsed:0.000} ms";

        [JsonProperty("@l", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Level { get; }

        [JsonProperty("@r")]
        public string[] Renderings => new[] {Elapsed.ToString("0.000", CultureInfo.InvariantCulture)};

        public string HealthCheckTitle { get; }
        public string Method { get; }
        public string TargetUrl { get; }
        public string Outcome { get; }
        public double Elapsed { get; }
        public int? StatusCode { get; }
        public string ContentType { get; }
        public long? ContentLength { get; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string InitialContent { get; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public JValue Data { get; }

        public HealthCheckResult(
            DateTime utcTimestamp,
            string healthCheckTitle,
            string method,
            string targetUrl,            
            string outcome,
            string level,
            double elapsed,
            int? statusCode,
            string contentType,
            long? contentLength,
            string initialContent,
            Exception exception,
            JValue data)
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
            Exception = exception?.ToString();
            Data = data;
        }
    }
}
