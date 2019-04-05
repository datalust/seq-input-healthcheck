using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Seq.Input.HealthCheck
{
    class HttpHealthCheck
    {
        readonly string _title;
        readonly string _targetUrl;
        readonly JsonDataExtractor _extractor;
        readonly HttpClient _httpClient;
        readonly byte[] _buffer = new byte[2048];

        static readonly UTF8Encoding ForgivingEncoding = new UTF8Encoding(false, false);
        const int InitialContentChars = 16;

        public HttpHealthCheck(HttpClient httpClient, string title, string targetUrl, JsonDataExtractor extractor)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _title = title ?? throw new ArgumentNullException(nameof(title));
            _targetUrl = targetUrl ?? throw new ArgumentNullException(nameof(targetUrl));
            _extractor = extractor;
        }

        public async Task<HealthCheckResult> CheckNow(CancellationToken cancel)
        {
            string outcome;

            Exception exception = null;
            int? statusCode = null;
            string contentType = null;
            long? contentLength = null;
            string initialContent = null;
            JToken data = null;

            var utcTimestamp = DateTime.UtcNow;
            var sw = Stopwatch.StartNew();

            try
            {
                var response = await _httpClient.GetAsync(_targetUrl, cancel);
                statusCode = (int)response.StatusCode;
                contentType = response.Content.Headers.ContentType?.ToString();
                contentLength = response.Content.Headers.ContentLength;

                var content = await response.Content.ReadAsStreamAsync();
                (initialContent, data) = await DownloadContent(content, contentType, contentLength);

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
                exception,
                data);
        }

        // Either initial content, or extracted data
        async Task<(string initialContent, JToken data)> DownloadContent(Stream body, string contentType, long? contentLength)
        {
            if (_extractor == null ||
                contentLength == 0 ||
                contentType != "application/json; charset=utf-8" && contentType != "application/json")
            {
                var read = await body.ReadAsync(_buffer, 0, _buffer.Length);
                var initial = ForgivingEncoding.GetString(_buffer, 0, Math.Min(read, InitialContentChars));

                // Drain the response to avoid dropped connection errors on the server.
                while (read > 0)
                    read = await body.ReadAsync(_buffer, 0, _buffer.Length);

                return (initial, null);
            }

            var reader = new StreamReader(body, ForgivingEncoding);
            if (!reader.EndOfStream && reader.Peek() != '{')
            {
                // Not the happy path
                var chars = new char[InitialContentChars];
                var read = await reader.ReadAsync(chars, 0, InitialContentChars);

                // Drain the response; note this is reading directly from the stream and not the
                // reader over it.
                while (read > 0)
                    read = await body.ReadAsync(_buffer, 0, _buffer.Length);

                return (new string(chars), null);
            }

            return (null, _extractor.ExtractData(reader));
        }
    }
}