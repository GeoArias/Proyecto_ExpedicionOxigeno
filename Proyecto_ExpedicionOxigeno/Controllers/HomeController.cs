// HomeController.cs
using Proyecto_ExpedicionOxigeno.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web.Mvc;

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
        // POST: Home/GuardarContacto (guardar contacto)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarContacto(Contacto contacto)
        {
            if (ModelState.IsValid)
            {
                contacto.Fecha = DateTime.Now;
                db.Contactos.Add(contacto);
                db.SaveChanges();
                TempData["MensajeEnviado"] = "¡Consulta enviada correctamente!";
                return RedirectToAction("Index");
            }

            var reseñas = db.Reviews.OrderByDescending(r => r.Fecha).ToList();
            return View("Index", reseñas);
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
