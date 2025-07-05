using Proyecto_ExpedicionOxigeno.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        [HttpGet]
        public ActionResult Index(int? calificacion, string servicio)
        {
            var reviewsQuery = db.Reviews.Where(r => r.Mostrar);

            if (calificacion.HasValue)
            {
                reviewsQuery = reviewsQuery.Where(r => r.Calificacion >= calificacion.Value);
            }

            if (!string.IsNullOrEmpty(servicio))
            {
                reviewsQuery = reviewsQuery.Where(r => r.Servicio == servicio);
            }

            var reviews = reviewsQuery
                .OrderByDescending(r => r.Fecha)
                .Take(5)
                .ToList();

            ViewBag.Reviews = reviews;
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Index(Review review)
        {
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var nombre = claimsIdentity.FindFirst("Nombre")?.Value ?? "Anónimo";

                review.Nombre = nombre;
                review.Fecha = DateTime.Now;
                review.Mostrar = false;

                db.Reviews.Add(review);
                db.SaveChanges();

                TempData["ResenaGuardada"] = true;
                return RedirectToAction("Index");
            }

            // Si el modelo no es válido, volver a cargar las reseñas mostradas
            ViewBag.Reviews = db.Reviews
                .Where(r => r.Mostrar)
                .OrderByDescending(r => r.Fecha)
                .Take(5)
                .ToList();

            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarContacto(Contacto contacto)
        {
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var nombre = claimsIdentity.FindFirst("Nombre")?.Value ?? "Anónimo";

                contacto.Nombre = nombre;
                contacto.Fecha = DateTime.Now;

                db.Contactos.Add(contacto);
                db.SaveChanges();

                TempData["MensajeEnviado"] = "¡Consulta enviada correctamente!";
                return RedirectToAction("Index");
            }

            ViewBag.Reviews = db.Reviews
                .Where(r => r.Mostrar)
                .OrderByDescending(r => r.Fecha)
                .Take(5)
                .ToList();

            return View("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
