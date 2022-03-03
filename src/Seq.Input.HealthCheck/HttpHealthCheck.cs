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
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Seq.Input.HealthCheck.Util;

namespace Seq.Input.HealthCheck
{
    class HttpHealthCheck
    {
        readonly string _title;
        readonly string _targetUrl;
        readonly string _authenticationHeaderValue;
        readonly JsonDataExtractor _extractor;
        readonly bool _bypassHttpCaching;
        readonly HttpClient _httpClient;
        readonly byte[] _buffer = new byte[2048];

        public const string ProbeIdParameterName = "__probe";

        static readonly UTF8Encoding ForgivingEncoding = new UTF8Encoding(false, false);
        const int InitialContentChars = 16;
        const string OutcomeSucceeded = "succeeded", OutcomeFailed = "failed";

        public HttpHealthCheck(HttpClient httpClient, string title, string targetUrl, string authenticationHeaderValue, JsonDataExtractor extractor, bool bypassHttpCaching)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _title = title ?? throw new ArgumentNullException(nameof(title));
            _targetUrl = targetUrl ?? throw new ArgumentNullException(nameof(targetUrl));
            _authenticationHeaderValue = authenticationHeaderValue;
            _extractor = extractor;
            _bypassHttpCaching = bypassHttpCaching;
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

            var probeId = Nonce.Generate(12);
            var probedUrl = _bypassHttpCaching ?
                UrlHelper.AppendParameter(_targetUrl, ProbeIdParameterName, probeId) :
                _targetUrl;

            var utcTimestamp = DateTime.UtcNow;
            var sw = Stopwatch.StartNew();

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, probedUrl);
                request.Headers.Add("X-Correlation-ID", probeId);
                if (_authenticationHeaderValue != null)
                {
                    request.Headers.Add("Authorization", _authenticationHeaderValue);
                }

                if (_bypassHttpCaching)
                    request.Headers.CacheControl = new CacheControlHeaderValue { NoStore = true };

                var response = await _httpClient.SendAsync(request, cancel);
                
                statusCode = (int)response.StatusCode;
                contentType = response.Content.Headers.ContentType?.ToString();
                contentLength = response.Content.Headers.ContentLength;

                var content = await response.Content.ReadAsStreamAsync();
                (initialContent, data) = await DownloadContent(content, contentType, contentLength);

                outcome = response.IsSuccessStatusCode ? OutcomeSucceeded : OutcomeFailed;
            }
            catch (Exception ex)
            {
                outcome = OutcomeFailed;
                exception = ex;
            }

            sw.Stop();

            var level = outcome == OutcomeFailed ? "Error" :
                data == null && _extractor != null ? "Warning" :
                null;

            return new HealthCheckResult(
                utcTimestamp,
                _title,
                "GET",
                _targetUrl,
                outcome,
                probeId,
                level,
                sw.Elapsed.TotalMilliseconds,
                statusCode,
                contentType,
                contentLength,
                initialContent,
                exception,
                data,
                _targetUrl == probedUrl ? null : probedUrl);
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