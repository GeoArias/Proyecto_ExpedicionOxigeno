using System.Linq;
using System.Threading.Tasks;
using Proyecto_ExpedicionOxigeno.Models;
using System.Data.Entity;

namespace Proyecto_ExpedicionOxigeno.Extensions
{
    public static class SelloExtensions
    {
        // Extensión para ApplicationUser para obtener sellos fácilmente
        public static async Task<int> ObtenerSellosDisponiblesAsync(this ApplicationUser user, ApplicationDbContext context)
        {
            return await context.Sellos
                .Where(s => s.UserId == user.Id && !s.UsadoEnPase)
                .CountAsync();
        }


        // Extensión para verificar si puede obtener pase
        public static async Task<bool> PuedeObtenerPaseAsync(this ApplicationUser user, ApplicationDbContext context)
        {
            var sellosDisponibles = await user.ObtenerSellosDisponiblesAsync(context);
            return sellosDisponibles >= 5;
        }
    }
}