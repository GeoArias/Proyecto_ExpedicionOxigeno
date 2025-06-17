namespace Proyecto_ExpedicionOxigeno.Migrations
{
    using Microsoft.AspNet.Identity.EntityFramework;
    using Microsoft.AspNet.Identity;
    using Proyecto_ExpedicionOxigeno.Models;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Proyecto_ExpedicionOxigeno.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(Proyecto_ExpedicionOxigeno.Models.ApplicationDbContext context)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

            // Crear roles si no existen
            if (!roleManager.RoleExists("Administrador"))
                roleManager.Create(new IdentityRole("Administrador"));
            if (!roleManager.RoleExists("Usuario"))
                roleManager.Create(new IdentityRole("Usuario"));

            // Crear usuario administrador por defecto
            var adminEmail = "admin@demo.com";
            var adminUser = userManager.FindByName(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                userManager.Create(adminUser, "Admin123!"); // Cambia la contraseña si lo deseas
                userManager.AddToRole(adminUser.Id, "Administrador");
            }

            base.Seed(context);
        }
    }
}
