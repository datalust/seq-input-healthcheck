using System.Net.Http;

namespace Seq.Input.HealthCheck
{
    public static class HttpHealthCheckClient
    {
        public static HttpClient Create()
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = false };
            var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Connection.Add("Close");
            return httpClient;
        }
    }
}
