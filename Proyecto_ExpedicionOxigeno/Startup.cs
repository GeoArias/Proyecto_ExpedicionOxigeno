using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Proyecto_ExpedicionOxigeno.Startup))]
namespace Proyecto_ExpedicionOxigeno
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            // Iniciar conexión con Graph
            ConectionGraph();
        }
    }
}
