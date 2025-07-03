using Microsoft.Identity.Client;
using Proyecto_ExpedicionOxigeno.Models;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace Proyecto_ExpedicionOxigeno.Helpers
{
    public static class GraphTokenProvider
    {
        private static readonly string appId = ConfigurationManager.AppSettings["ida:AppId"];
        private static readonly string appSecret = ConfigurationManager.AppSettings["ida:AppSecret"];
        private static readonly string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        private static readonly string graphScopes = "https://graph.microsoft.com/.default";

        public static async Task<string> GetTokenAsync()
        {
            if (GraphTokenCache.IsTokenValid())
                return GraphTokenCache.Token;

            var authority = $"https://login.microsoftonline.com/{tenantId}";
            var app = ConfidentialClientApplicationBuilder
                .Create(appId)
                .WithClientSecret(appSecret)
                .WithAuthority(authority)
                .Build();

            string[] scopes = graphScopes.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

            GraphTokenCache.Token = result.AccessToken;
            GraphTokenCache.ExpiresOn = result.ExpiresOn.UtcDateTime;

            return result.AccessToken;
        }
    }
}