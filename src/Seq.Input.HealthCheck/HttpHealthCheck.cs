using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Seq.Input.HealthCheck
{
    class HttpHealthCheck : IDisposable
    {
        readonly string _title;
        readonly string _targetUrl;
        readonly HttpClient _httpClient;
        readonly byte[] _buffer = new byte[2048];

        static readonly UTF8Encoding ForgivingEncoding = new UTF8Encoding(false, false);

        public HttpHealthCheck(string title, string targetUrl)
        {
            _title = title ?? throw new ArgumentNullException(nameof(title));
            _targetUrl = targetUrl ?? throw new ArgumentNullException(nameof(targetUrl));
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Connection.Add("Close");
        }

        public async Task<HealthCheckResult> CheckNow(CancellationToken cancel)
        {
            string outcome;

            Exception exception = null;
            int? statusCode = null;
            string contentType = null;
            long? contentLength = null;
            string initialContent = null;

            var utcTimestamp = DateTime.UtcNow;
            var sw = Stopwatch.StartNew();

            try
            {
                var response = await _httpClient.GetAsync(_targetUrl, cancel);
                statusCode = (int)response.StatusCode;
                contentType = response.Content.Headers.ContentType?.ToString();
                contentLength = response.Content.Headers.ContentLength;

                var content = await response.Content.ReadAsStreamAsync();
                initialContent = await DownloadContent(content);

                outcome = response.IsSuccessStatusCode ? "succeeded" : "failed";
            }
            catch (Exception ex)
            {
                outcome = "failed";
                exception = ex;
            }

            sw.Stop();
            return new HealthCheckResult(
                utcTimestamp,
                _title,
                "GET",
                _targetUrl,
                outcome,
                outcome == "failed" ? "Error" : null,
                sw.Elapsed.TotalMilliseconds,
                statusCode,
                contentType,
                contentLength,
                initialContent,
                exception);
        }

        async Task<string> DownloadContent(Stream body)
        {
            var read = await body.ReadAsync(_buffer, 0, _buffer.Length);
            var initial = ForgivingEncoding.GetString(_buffer, 0, Math.Min(read, 16));
            while (read > 0)
            {
                read = await body.ReadAsync(_buffer, 0, _buffer.Length);
            }

            return initial;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}