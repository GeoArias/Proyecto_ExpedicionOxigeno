using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Linq;
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
            // Verificar si el usuario es administrador
            if (!User.IsInRole("Administrador"))
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










        // GET: Administracion/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }


        // GET: Administracion/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Administracion/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Administracion/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Administracion/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Administracion/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Administracion/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //
        //      Roles
        //

        // GET: Administracion/EditarRolUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarRolUsuario(string UserId, string SelectedRole)
        {
            if (!User.IsInRole("Administrador"))
            {
                return new HttpUnauthorizedResult();
            }

            var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var user = userManager.FindById(UserId);
            if (user == null)
            {
                return HttpNotFound();
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
            // Verificar si el usuario es administrador
            if (!User.IsInRole("Administrador"))
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
            // Verifica que el usuario sea administrador
            if (!User.IsInRole("Administrador"))
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
            // Verificar si el usuario es administrador
            if (!User.IsInRole("Administrador"))
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




    }
}
