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
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Seq.Input.HealthCheck.Data;
using Seq.Input.HealthCheck.Util;
using Serilog;

namespace Seq.Input.HealthCheck
{
    record Redirect(Uri? RequestUri, int StatusCode, Uri RedirectUri);

    class HttpHealthCheck
    {
        readonly string _title;
        readonly string _targetUrl;
        readonly List<(string, string)> _headers;
        readonly JsonDataExtractor? _extractor;
        readonly bool _bypassHttpCaching;
        readonly HttpClient _httpClient;
        readonly bool _shouldFollowRedirects;
        readonly byte[] _buffer = new byte[2048];

        public const string ProbeIdParameterName = "__probe";
        public const string CorrelationHeaderId = "X-Correlation-ID";
        public const int MaxRedirectCount = 10;

        static readonly UTF8Encoding ForgivingEncoding = new(false, false);
        readonly ILogger _log;
        const int InitialContentChars = 16;
        const string OutcomeSucceeded = "succeeded", OutcomeFailed = "failed";

        public HttpHealthCheck(HttpClient httpClient, string title, string targetUrl, List<(string, string)> headers, JsonDataExtractor? extractor,
            bool bypassHttpCaching, ILogger log, bool shouldFollowRedirects = false)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _title = title ?? throw new ArgumentNullException(nameof(title));
            _targetUrl = targetUrl ?? throw new ArgumentNullException(nameof(targetUrl));
            _headers = headers;
            _extractor = extractor;
            _bypassHttpCaching = bypassHttpCaching;
            _shouldFollowRedirects = shouldFollowRedirects;
            // todo: Is this the best way to inject logging?
            _log = log;
        }

        public async Task<HealthCheckResult> CheckNow(CancellationToken cancel)
        {
            string outcome;

            Exception? exception = null;
            int? statusCode = null;
            string? contentType = null;
            long? contentLength = null;
            string? initialContent = null;
            JToken? data = null;
            Stack<Redirect> _redirectedUrls = new();


            var probeId = Nonce.Generate(12);

            // todo: Does bypassing the _httpCaching have any impact with regard to following redirects?
            var probedUrl = _bypassHttpCaching ? UrlHelper.AppendParameter(_targetUrl, ProbeIdParameterName, probeId) : _targetUrl;
            var utcTimestamp = DateTime.UtcNow;
            var sw = Stopwatch.StartNew();

            try
            {
                var response = await MakeAndFollowRequest(cancel, probedUrl, probeId, _redirectedUrls);

                statusCode = (int) response.StatusCode;
                contentType = response.Content.Headers.ContentType?.ToString();
                contentLength = response.Content.Headers.ContentLength;

                var content = await response.Content.ReadAsStreamAsync(cancel);
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
            _redirectedUrls.TryPeek(out var topRedirect);
            var finalUrl = topRedirect?.RedirectUri.ToString();
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
                _targetUrl == probedUrl ? null : probedUrl,
                redirectCount: _redirectedUrls.Count,
                finalUrl: finalUrl);
        }

        private void AddHeadersToRequest(HttpRequestMessage request, string probeId)
        {
            request.Headers.Add(CorrelationHeaderId, probeId);
            foreach (var (name, value) in _headers)
            {
                // This will throw if a header is duplicated (better for the user to detect this configuration problem).
                request.Headers.Add(name, value);
            }

            if (_bypassHttpCaching)
                request.Headers.CacheControl = new CacheControlHeaderValue {NoStore = true};
        }

        private async Task<HttpResponseMessage> MakeAndFollowRequest(CancellationToken cancel, string requestUri, string correlationId,
            Stack<Redirect> previousRedirects)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            // include initial headers on every redirect
            // todo: Should any response headers included in future requests?
            AddHeadersToRequest(request, correlationId);

            var response = await _httpClient.SendAsync(request, cancel);
            var statusCode = (int) response.StatusCode;
            var exceededRedirectCount = previousRedirects.Count > MaxRedirectCount;
            var responseStatusCodeWas300 = statusCode is >= 300 and <= 399;

            if (_shouldFollowRedirects && responseStatusCodeWas300 && !exceededRedirectCount)
            {
                var locationHeader = response.Headers.Location;
                // https://httpwg.org/specs/rfc9110.html#status.3xx
                // Redirects that indicate this resource might be available at a different URI:
                // 301 (Moved Permanently),
                // 302 (Found),
                // 303 (See Other),
                // 307 (Temporary Redirect),
                // 308 (Permanent Redirect)
                if (locationHeader is not null)
                {
                    // todo: do we want to log the full stack of redirects that occured anywhere?
                    previousRedirects.Push(new Redirect(request.RequestUri, statusCode, locationHeader));
                    // todo: Should we rely on the server to include the resource as well as query parameters...
                    // or should we analyze and ensure that they're included?
                    var newUri = locationHeader.ToString();
                    return await MakeAndFollowRequest(cancel, newUri, correlationId, previousRedirects);
                }

                // todo what to do if there's not a `location` header? as in 300 and 304?
                // https://httpwg.org/specs/rfc9110.html#status.3xx
                // Redirection that offers a choice among matching resources capable of representing this resource,
                // as in the 300 (Multiple Choices) status code.
                // as in the 304 (Not Modified) status code.
                else
                {
                    // todo: Should this be a warning or an exception?
                    // todo: Should we destructure the Request & Response?
                    _log.Warning(
                        "Http Redirect Status Code {StatusCode} Received with No Location Header {Header} from response {@Response} during request {@Request}",
                        statusCode,
                        locationHeader, response, request);
                }
            }

            return response;
        }

        // Either initial content, or extracted data
        async Task<(string? initialContent, JToken? data)> DownloadContent(Stream body, string? contentType, long? contentLength)
        {
            if (_extractor == null ||
                contentLength == 0 ||
                contentType != "application/json; charset=utf-8" && contentType != "application/json")
            {
                var read = await body.ReadAsync(_buffer);
                var initial = ForgivingEncoding.GetString(_buffer, 0, Math.Min(read, InitialContentChars));

                // Drain the response to avoid dropped connection errors on the server.
                while (read > 0)
                    read = await body.ReadAsync(_buffer);

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