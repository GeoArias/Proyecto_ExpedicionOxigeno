using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using Proyecto_ExpedicionOxigeno.Models;
using System;
using System.Configuration;

namespace Proyecto_ExpedicionOxigeno
{
    public partial class Startup
    {
        // Traer los datos para conexión a Graph
        private static string appId = ConfigurationManager.AppSettings["ida:AppId"];
        private static string appSecret = ConfigurationManager.AppSettings["ida:AppSecret"];
        private static string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        private static string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
        private static string graphScopes = "https://graph.microsoft.com/.default";


        public void ConectionGraph()
        {
            // La lógica implementada es:
            // 1. Crear un objeto ConfidentialClientApplication usando appId, appSecret, redirectUri y tenantId.
            // 2. Definir los scopes separando graphScopes por espacio o coma.
            // 3. Intentar adquirir un token para la aplicación (client credentials flow).
            // 4. Manejar excepciones si la autenticación falla.

            try
            {
                var authority = $"https://login.microsoftonline.com/{tenantId}";

                var app = Microsoft.Identity.Client.ConfidentialClientApplicationBuilder
                    .Create(appId)
                    .WithClientSecret(appSecret)
                    .WithRedirectUri(redirectUri)
                    .WithAuthority(authority)
                    .Build();

                string[] scopes = graphScopes.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

                // Adquirir token para la aplicación (client credentials)
                var result = app.AcquireTokenForClient(scopes)
                                .ExecuteAsync().GetAwaiter().GetResult();

                // Puedes almacenar el token en una variable estática o en caché según tus necesidades
                GraphTokenCache.Token = result.AccessToken;
            }
            catch (Exception ex)
            {
                // Manejo de errores de autenticación
                // Puedes registrar el error o lanzar una excepción personalizada
                System.Diagnostics.Debug.WriteLine("Error al conectar a Microsoft Graph: " + ex.Message);
                throw;
            }
        }


        // Para obtener más información sobre cómo configurar la autenticación, visite https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Configure el contexto de base de datos, el administrador de usuarios y el administrador de inicios de sesión para usar una única instancia por solicitud
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);


            // Permitir que la aplicación use una cookie para almacenar información para el usuario que inicia sesión
            // y una cookie para almacenar temporalmente información sobre un usuario que inicia sesión con un proveedor de inicio de sesión de terceros
            // Configurar cookie de inicio de sesión
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                Provider = new CookieAuthenticationProvider
                {
                    // Permite a la aplicación validar la marca de seguridad cuando el usuario inicia sesión.
                    // Es una característica de seguridad que se usa cuando se cambia una contraseña o se agrega un inicio de sesión externo a la cuenta.  
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Permite que la aplicación almacene temporalmente la información del usuario cuando se verifica el segundo factor en el proceso de autenticación de dos factores.
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));

            // Permite que la aplicación recuerde el segundo factor de verificación de inicio de sesión, como el teléfono o correo electrónico.
            // Cuando selecciona esta opción, el segundo paso de la verificación del proceso de inicio de sesión se recordará en el dispositivo desde el que ha iniciado sesión.
            // Es similar a la opción Recordarme al iniciar sesión.
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            // Quitar los comentarios de las siguientes líneas para habilitar el inicio de sesión con proveedores de inicio de sesión de terceros
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //   consumerKey: "",
            //   consumerSecret: "");

            //app.UseFacebookAuthentication(
            //   appId: "",
            //   appSecret: "");

            //app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            //{
            //    ClientId = "",
            //    ClientSecret = ""
            //});
        }
    }
}