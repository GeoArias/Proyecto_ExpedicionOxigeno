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
            //  
            // Roles  
            //  
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
                userManager.Create(adminUser, "Admin123!"); 
                userManager.AddToRole(adminUser.Id, "Administrador");
            }

            //  
            // Estados  
            //  
            var estados = new[]
            {
               new Estado { Id = 0, Nombre = "Inactivo" },
               new Estado { Id = 1, Nombre = "Activo" },
               new Estado { Id = 3, Nombre = "Canjeado" },
               new Estado { Id = 4, Nombre = "Expirado" }
           };

            foreach (var estado in estados)
            {
                if (!context.Estados.Any(e => e.Id == estado.Id))
                {
                    context.Estados.Add(estado);
                }
            }

            context.SaveChanges();



            base.Seed(context);
        }
    }
}
