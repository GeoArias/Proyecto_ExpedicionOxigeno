using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;


namespace Proyecto_ExpedicionOxigeno.Models
{
    // Para agregar datos de perfil del usuario, agregue más propiedades a su clase ApplicationUser. Visite https://go.microsoft.com/fwlink/?LinkID=317594 para obtener más información.
    public class ApplicationUser : IdentityUser
    {
        public string Nombre { get; internal set; }
        public string Telefono { get; internal set; }
        public Guid MsBookingsId { get; internal set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Tenga en cuenta que authenticationType debe coincidir con el valor definido en CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Agregar reclamaciones de usuario personalizadas aquí
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<Estado> Estados { get; set; }
        public DbSet<Contacto> Contactos { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<PuntosFidelidad> PuntosFidelidad { get; set; }
        public DbSet<Sello> Sellos { get; set; }
        public DbSet<CodigoQR> CodigosQR { get; set; }
        public DbSet<ValidacionQR> ValidacionesQR { get; set; }
        public DbSet<PaseExpedicion> PasesExpedicion { get; set; }
    }
}
