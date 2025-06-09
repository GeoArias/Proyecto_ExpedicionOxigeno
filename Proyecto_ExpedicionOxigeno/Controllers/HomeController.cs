using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Proyecto_ExpedicionOxigeno.Models; // Asegúrate de tener esta línea para acceder al modelo y contexto

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    public class HomeController : Controller
    {
        private ExpediCheckContext db = new ExpediCheckContext(); // Conexión a la base de datos

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        // GET: Reseñas
        [HttpGet]
        public ActionResult Resenas()
        {
            List<Review> reseñas = db.Reviews.OrderByDescending(r => r.Fecha).ToList();
            return View(reseñas);
        }

        // POST: Reseñas
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Resenas(Review review)
        {
            if (ModelState.IsValid)
            {
                review.Fecha = DateTime.Now; // Establecer fecha actual
                db.Reviews.Add(review);
                db.SaveChanges();

                TempData["ResenaGuardada"] = true; // ✅ Activa el popup en la vista
                return RedirectToAction("Resenas");
            }

            // Si hay errores, se vuelve a cargar la lista de reseñas existentes
            var reseñas = db.Reviews.OrderByDescending(r => r.Fecha).ToList();
            return View(reseñas);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose(); // Liberar la conexión a la base de datos
            }
            base.Dispose(disposing);
        }
    }
}
