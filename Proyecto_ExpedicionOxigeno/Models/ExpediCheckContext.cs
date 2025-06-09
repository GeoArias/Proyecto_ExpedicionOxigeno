using System.Data.Entity;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class ExpediCheckContext : DbContext
    {
        // Constructor que usa tu cadena de conexión llamada "DefaultConnection"
        public ExpediCheckContext()
            : base("DefaultConnection")
        {
        }

        // DbSet para la tabla de reseñas (reviews)
        public DbSet<Review> Reviews { get; set; }
    }
}
