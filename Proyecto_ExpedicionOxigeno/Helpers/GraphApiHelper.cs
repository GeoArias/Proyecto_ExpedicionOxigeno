using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Proyecto_ExpedicionOxigeno.Helpers
{
    public static class GraphApiHelper
    {
        public static async Task<HttpResponseMessage> SendGraphRequestAsync(string url, HttpMethod method, HttpContent content = null)
        {
            using (var client = new HttpClient())
            {
                var token = await GraphTokenProvider.GetTokenAsync();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var request = new HttpRequestMessage(method, url);
                if (content != null)
                    request.Content = content;
                var result = await client.SendAsync(request);
                return result;
            }
        }
    }
}