using System.Web.Mvc;
using System.Web.Routing;

namespace Proyecto_ExpedicionOxigeno
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            // Rutas para sistema de sellos
            routes.MapRoute(
                name: "SelloValidacion",
                url: "sello/validar",
                defaults: new { controller = "Sello", action = "ValidarQR" }
            );

            routes.MapRoute(
                name: "SelloEscanear",
                url: "sello/escanear",
                defaults: new { controller = "Sello", action = "EscanearQR" }
            );

            routes.MapRoute(
                name: "BookingsWebhook",
                url: "webhook/bookings",
                defaults: new { controller = "Webhook", action = "BookingsWebhook" }
            );
        }
    }
}
