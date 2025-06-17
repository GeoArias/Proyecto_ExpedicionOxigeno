// HomeController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Proyecto_ExpedicionOxigeno.Models;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    public class HomeController : Controller
    {
        private ExpediCheckContext db = new ExpediCheckContext();

        // GET: Home/Index
        [HttpGet]
        public ActionResult Index()
        {
            var reseñas = db.Reviews.OrderByDescending(r => r.Fecha).ToList();
            return View(reseñas);
        }

        // POST: Home/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(Review review)
        {
            if (ModelState.IsValid)
            {
                review.Fecha = DateTime.Now;
                db.Reviews.Add(review);
                db.SaveChanges();
                TempData["ResenaGuardada"] = true;
                return RedirectToAction("Index");
            }

            var reseñas = db.Reviews.OrderByDescending(r => r.Fecha).ToList();
            return View(reseñas);
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
