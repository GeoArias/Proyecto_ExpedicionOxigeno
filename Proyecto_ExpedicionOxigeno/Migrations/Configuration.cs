namespace Proyecto_ExpedicionOxigeno.Migrations
{
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using Proyecto_ExpedicionOxigeno.Models;
    using System;
    using System.Configuration;
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
            if (!roleManager.RoleExists("Empleado"))
                roleManager.Create(new IdentityRole("Empleado"));

            // Crear usuario administrador por defecto  
            var adminEmail = ConfigurationManager.AppSettings["ida:MSFTBookingsAdministradorPrimario"].ToLower().Trim();
            var adminUser = userManager.FindByName(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, Nombre = "Geovanny", Telefono= "61426278", MsBookingsId= Guid.Parse("59acd6a8-2ba4-4c00-ba2a-820220ad1078") };
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

            //
            //  Crear Consultas (Contactos) por defecto
            //
            var consultas = new[]
            {
                new Contacto
                {
                    Id = 1,
                   Nombre = "Geo Arias",
                   Consulta = "¿Cuáles son los horarios disponibles para escalar?",
                   Telefono = "61426278",
                   Email = "geoas121cr@gmail.com",
                   Fecha = DateTime.Now,
                   Respondida = false
                },
                new Contacto
               {
                   Id = 2,
                   Nombre = "Ana Gómez",
                   Consulta = "¿Es necesario llevar mi propio equipo de escalada?",
                   Telefono = "0987654321",
                   Email = "ana.gomez@example.com",
                   Fecha = DateTime.Now,
                   Respondida = false
               },
                new Contacto
               {
                   Id = 3,
                   Nombre = "Carlos López",
                   Consulta = "¿Cuánto cuesta una sesión de escalada?",
                   Telefono = "1122334455",
                   Email = "carlos.lopez@example.com",
                   Fecha = DateTime.Now,
                   Respondida = false
               }
                };

            foreach (var consulta in consultas)
            {
                if (!context.Contactos.Any(c => c.Id == consulta.Id))
                {
                    context.Contactos.Add(consulta);
                }
            }

            context.SaveChanges();

            //
            //  Crear Reseñas de Ejemplos
            //
            var reseñas = new[]
            {
                new Review
                {
                    Id = 1,
                    Nombre = "Geo Arias",
                    Comentario = "¡Una experiencia increíble! Los guías son muy profesionales y la decoración es increíble",
                    Fecha = DateTime.Now,
                    Mostrar= true,
                    Calificacion = 5,
                    Servicio = "Parque de Cuerdas"
                },
                new Review
                {
                    Id = 2,
                    Nombre = "Ana Gómez",
                    Comentario = "Me encantó la experiencia, pero creo que deberían mejorar la señalización en algunas rutas.",
                    Fecha = DateTime.Now,
                    Mostrar= true,
                    Calificacion = 4,
                    Servicio = "Pared de Escalada"
                },
                new Review
                {
                    Id = 3,
                    Nombre = "Carlos López",
                    Comentario = "Excelente atención y servicio. Definitivamente volveré.",
                    Fecha = DateTime.Now,
                    Mostrar= false,
                    Calificacion = 5,
                    Servicio = "Pared de Escalada"
                }
            };
            foreach (var reseña in reseñas)
            {
                if (!context.Reviews.Any(r => r.Id == reseña.Id))
                {
                    context.Reviews.Add(reseña);
                }
            }
            context.SaveChanges();




            base.Seed(context);
        }
    }
}
