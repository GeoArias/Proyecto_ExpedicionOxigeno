using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Graph.Models;
using Proyecto_ExpedicionOxigeno.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    public class AdministracionController : Controller
    {
        private ApplicationUserManager _userManager;
        private EmailService emailService = new EmailService();
        string logoUrl;
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            logoUrl = Url.Content("~/Resources/Images/logo.png");
            if (!logoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                logoUrl = $"{Request.Url.Scheme}://{Request.Url.Authority}{logoUrl}";
            }
        }
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        // GET: Administracion
        public ActionResult Index()
        {
            return View();
        }

        //
        //  Usuarios
        //

        //
        // GET: /Administracion/Usuarios
        public ActionResult Usuarios()
        {
            // Verificar si el usuario es administrador o empleado
            if (!(User.IsInRole("Administrador") || User.IsInRole("Empleado")))
            {
                return new HttpUnauthorizedResult();
            }
            // Obtener la lista de usuarios
            var users = UserManager.Users.ToList();

            // Obtener la lista de roles desde Identity
            using (var context = new Proyecto_ExpedicionOxigeno.Models.ApplicationDbContext())
            {
                var roleStore = new RoleStore<IdentityRole>(context);
                var roleManager = new RoleManager<IdentityRole>(roleStore);
                var roles = roleManager.Roles.Select(r => r.Name).ToList();
                ViewBag.Roles = roles;
            }
            return View(users);
        }

        //
        // POST:  Administracion/EliminarUsuario/IdUsuario
        [HttpPost]
        public ActionResult EliminarUsuario(string id)
        {
            if (!User.IsInRole("Administrador"))
            {
                return new HttpUnauthorizedResult();
            }

            var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var user = userManager.FindById(id);
            if (user == null)
            {
                TempData["Mensaje"] = "Usuario no encontrado.";
                return RedirectToAction("Usuarios");
            }
            if(user.Email.ToLower().Trim() == ConfigurationManager.AppSettings["ida:MSFTBookingsAdministradorPrimario"].ToLower().Trim())
            {
                TempData["Mensaje"] = "El administrador primario no se puede eliminar";
                return RedirectToAction("Usuarios");
            }

            // Eliminar el usuario
            var result = userManager.Delete(user);

            if (result.Succeeded)
            {
                TempData["Mensaje"] = "Usuario eliminado correctamente.";
            }
            else
            {
                TempData["Mensaje"] = "Error al eliminar el usuario: " + string.Join(", ", result.Errors);
            }

            return RedirectToAction("Usuarios");
        }



        //
        //      Roles
        //

        // GET: Administracion/EditarRolUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditarRolUsuario(string UserId, string SelectedRole)
        {
            if (!User.IsInRole("Administrador"))
            {
                return new HttpUnauthorizedResult();
            }
            



            var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            ApplicationUser user = userManager.FindById(UserId);
            if (user == null)
            {
                return HttpNotFound();
            }

            var previousRole = userManager.GetRoles(UserId).FirstOrDefault();


            // Conseguir el GUID del MS Bookings StaffMember y si no existe, crear uno nuevo
            BookingStaffMember staffWithEmail = await MSBookings_Actions.Get_MSBookingsStaffByEmail(user.Email);

            switch (previousRole)
            {
                case "Administrador":
                    if (user.Email.ToLower().Trim() != ConfigurationManager.AppSettings["ida:MSFTBookingsAdministradorPrimario"].ToLower().Trim())
                    {
                        if (SelectedRole == "Usuario" && staffWithEmail != null)
                        {

                            // Si el usuario era Administrador, y ahora Usuario, eliminarlo de MS Bookings
                            await MSBookings_Actions.Delete_MSBookingsStaff(staffWithEmail.Id);
                        }
                    }
                    else
                    {
                        TempData["Error"] = "No se puede eliminar el administrador primario.";
                        return RedirectToAction("Usuarios");
                    }
                    break;
                case "Usuario":
                    if ((SelectedRole == "Administrador" || SelectedRole == "Empleado") && staffWithEmail == null)
                    {
                        // Si el usuario era Usuario, y ahora Administrador o Empleado, crearlo en MS Bookings
                        var newStaff = new
                        {
                            DisplayName = user.Nombre,
                            EmailAddress = user.Email,
                            Role = "administrator",
                        };
                        HttpResponseMessage resultCreate = await MSBookings_Actions.Create_MSBookingsStaff(newStaff);
                        if (!resultCreate.IsSuccessStatusCode)
                        {
                            TempData["Error"] = "Error al crear Staff en MSBookings:\n" + resultCreate.ReasonPhrase;
                            return RedirectToAction("Usuarios");
                        }
                        staffWithEmail = await MSBookings_Actions.Get_MSBookingsStaffByEmail(user.Email);
                        user.MsBookingsId = Guid.Parse(staffWithEmail.Id);
                    }
                    break;
                case "Empleado":
                    if (SelectedRole == "Usuario" && staffWithEmail != null)
                    {
                        // Si el usuario era Empleado, y ahora Usuario, eliminarlo de MS Bookings
                        await MSBookings_Actions.Delete_MSBookingsStaff(staffWithEmail.Id);
                    }
                    break;
            }
            
            var result = userManager.Update(user);
            if (!result.Succeeded)
            {
                TempData["Mensaje"] = "Error al actualizar el ID de reservas: " + string.Join(", ", result.Errors);
            }




            // Obtener los roles actuales del usuario
            var currentRoles = userManager.GetRoles(UserId);

            // Quitar todos los roles actuales
            foreach (var role in currentRoles)
            {
                userManager.RemoveFromRole(UserId, role);
            }

            // Asignar el nuevo rol seleccionado
            if (!string.IsNullOrEmpty(SelectedRole))
            {
                userManager.AddToRole(UserId, SelectedRole);
            }


            TempData["Mensaje"] = "Rol actualizado correctamente.";
            return RedirectToAction("Usuarios");
        }

        //
        //  Consultas (Contactos)
        //
        // GET: Administracion/Consultas 
        [HttpGet]
        public ActionResult Consultas()
        {
            // Verificar si el usuario es administrador o empleado
            if (!(User.IsInRole("Administrador") || User.IsInRole("Empleado")))
            {
                return new HttpUnauthorizedResult();
            }
            // Obtener la lista de contactos
            using (var context = new Proyecto_ExpedicionOxigeno.Models.ApplicationDbContext())
            {
                var contactos = context.Contactos.ToList();
                return View(contactos);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResponderConsulta(int Id, string ParaEmail, string Respuesta)
        {
            // Verificar si el usuario es administrador o empleado
            if (!(User.IsInRole("Administrador") || User.IsInRole("Empleado")))
            {
                return new HttpUnauthorizedResult();
            }

            using (var context = new Proyecto_ExpedicionOxigeno.Models.ApplicationDbContext())
            {
                var consulta = context.Contactos.FirstOrDefault(c => c.Id == Id);
                if (consulta == null)
                {
                    TempData["Mensaje"] = "Consulta no encontrada.";
                    return RedirectToAction("Consultas");
                }

                // Marcar como respondida
                consulta.Respondida = true;
                context.SaveChanges();

                // Enviar correo
                var emailService = new EmailService();
                string asunto = "Respuesta a tu consulta - Expedición Oxígeno";
                string cuerpo = $@"
        <div style='font-family: Arial, sans-serif; max-width: 500px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; padding: 24px; background: #fafafa;'>
            <div style='text-align:center; margin-bottom:24px;'>
                <img src='{logoUrl}' alt='Expedición Oxígeno' style='max-width:180px; height:auto;' />
            </div>
            <h2 style='color: #2c3e50;'>¡Gracias por contactarnos!</h2>
            <p style='font-size: 16px; color: #333;'>Hemos recibido tu consulta y aquí tienes nuestra respuesta:</p>
            <div style='background: #e9f7ef; border-radius: 5px; padding: 16px; margin: 24px 0; color: #222;'>
                {System.Net.WebUtility.HtmlEncode(Respuesta).Replace("\n", "<br/>")}
            </div>
            <p style='font-size: 14px; color: #888;'>Si tienes más dudas, no dudes en responder este correo.</p>
            <hr style='border: none; border-top: 1px solid #eee; margin: 24px 0;' />
            <p style='font-size: 12px; color: #bbb;'>Expedición Oxígeno</p>
        </div>";

                // Await the asynchronous email sending operation
                await emailService.SendAsync(new Microsoft.AspNet.Identity.IdentityMessage
                {
                    Destination = ParaEmail,
                    Subject = asunto,
                    Body = cuerpo
                });

                TempData["Mensaje"] = "Respuesta enviada correctamente.";
                return RedirectToAction("Consultas");
            }
        }




        //
        //      Reviews
        //

        // GET: Administracion/Reviews
        [HttpGet]
        public ActionResult Reviews()
        {
            // Verificar si el usuario es administrador o empleado
            if (!(User.IsInRole("Administrador") || User.IsInRole("Empleado")))
            {
                return new HttpUnauthorizedResult();
            }
            // Obtener la lista de reviews
            using (var context = new Proyecto_ExpedicionOxigeno.Models.ApplicationDbContext())
            {
                var reviews = context.Reviews.ToList();
                return View(reviews);
            }

        }

        // POST: Administracion/CambiarMostrarReview/IdReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarMostrarReview(int id)
        {
            // Verificar si el usuario es administrador o empleado
            if (!(User.IsInRole("Administrador")))
            {
                return new HttpUnauthorizedResult();
            }
            using (var db = new Proyecto_ExpedicionOxigeno.Models.ApplicationDbContext())
            {
                var review = db.Reviews.Find(id);
                if (review == null)
                {
                    return HttpNotFound();
                }
                review.Mostrar = !review.Mostrar;
                db.SaveChanges();
                TempData["Mensaje"] = "El estado de visibilidad de la reseña se ha actualizado.";
            }
            return RedirectToAction("Reviews");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CanjearSellos(string userId)
        {
            if (!User.IsInRole("Administrador") && !User.IsInRole("Empleado"))
            {
                TempData["Error"] = "Usuario no cuenta con suficientes permisos";
                return RedirectToAction("Usuarios");
            }

            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Usuario no especificado";
                return RedirectToAction("Usuarios");
            }

            using (var db = new ApplicationDbContext())
            {
                var usuario = db.Users.FirstOrDefault(u => u.Id == userId);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado";
                    return RedirectToAction("Usuarios");
                }

                var sellos = db.Sellos
                    .Where(s => s.UserId == userId && !s.UsadoEnPase)
                    .OrderBy(s => s.FechaObtencion)
                    .Take(5)
                    .ToList();

                if (sellos.Count < 5)
                {
                    TempData["Error"] = "Usuario no tiene suficientes sellos por canjear";
                    return RedirectToAction("Usuarios");
                }

                foreach (var sello in sellos)
                {
                    sello.UsadoEnPase = true;
                }
                await db.SaveChangesAsync();

                TempData["Mensaje"] = "Sellos canjeados y pase generado correctamente.";
                return RedirectToAction("Usuarios");
            }
        }




    }
}
