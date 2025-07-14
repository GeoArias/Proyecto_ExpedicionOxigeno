using Proyecto_ExpedicionOxigeno.Models;
using Proyecto_ExpedicionOxigeno.Services;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Proyecto_ExpedicionOxigeno.App_Start
{
    public class SelloConfig
    {
        public static void RegisterServices()
        {
            // Registrar servicios en el contenedor de dependencias
            DependencyResolver.SetResolver(new SelloDependencyResolver());
        }
    }

    public class SelloDependencyResolver : IDependencyResolver
    {
        public object GetService(System.Type serviceType)
        {
            if (serviceType == typeof(SelloService))
            {
                return new SelloService(new ApplicationDbContext());
            }
            return null;
        }

        public System.Collections.Generic.IEnumerable<object> GetServices(System.Type serviceType)
        {
            return new List<object>();
        }
    }
}