using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    public class AdministracionController : Controller
    {
        private ApplicationUserManager _userManager;
        private EmailService emailService = new EmailService();
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

    }
}
