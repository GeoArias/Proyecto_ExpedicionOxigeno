using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Proyecto_ExpedicionOxigeno.Helpers
{
    public static class GraphApiHelper
    {
        private static int maxRetries = int.Parse(ConfigurationManager.AppSettings["ida:MaxRetryGraphCalls"]);
        public static async Task<HttpResponseMessage> SendGraphRequestAsync(string url, HttpMethod method, HttpContent content = null)
        {
            int retryCount = 0;

            while (true)
            {
                using (var client = new HttpClient())
                {
                    var token = await GraphTokenProvider.GetTokenAsync();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var request = new HttpRequestMessage(method, url);
                    if (content != null)
                        request.Content = content;
                    var result = await client.SendAsync(request);

                    if (result.StatusCode == System.Net.HttpStatusCode.InternalServerError && retryCount < maxRetries)
                    {
                        retryCount++;
                        await Task.Delay(500 * retryCount); // Exponential backoff
                        continue;
                    }

                    return result;
                }
            }
        }
    }
}