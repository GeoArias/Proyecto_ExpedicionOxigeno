using Microsoft.AspNet.Identity;
using Proyecto_ExpedicionOxigeno.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Review/ReviewUser?reservaId=XXXXX
        public async Task<ActionResult> ReviewUser(string reservaId)
        {
            var userEmail = User.Identity.Name;
            var appointments = await MSBookings_Actions.GetAppointmentsByEmail(userEmail);

            var appointment = appointments
                .FirstOrDefault(a => a.Id == reservaId && a.end?.dateTime < DateTime.Now);

            if (appointment == null)
            {
                ViewBag.Error = "No se encontró una reserva pasada con ese identificador.";
                return View("ReviewUser");
            }

            ViewBag.ServiceName = appointment.ServiceName;
            ViewBag.ReservaId = reservaId;
            return View();
        }

        // POST: Review/ReviewUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReviewUser(string reservaId, int calificacion, string comentario)
        {
            var userEmail = User.Identity.Name;
            var userName = User.Identity.GetUserName();

            var appointments = MSBookings_Actions.GetAppointmentsByEmail(userEmail).Result;
            var appointment = appointments
                .FirstOrDefault(a => a.Id == reservaId && a.end?.dateTime < DateTime.Now);

            if (appointment == null)
            {
                TempData["Error"] = "No se encontró una reserva pasada con ese identificador.";
                return RedirectToAction("ReviewUser", new { reservaId });
            }

            // Guardar la reseña
            var db = new ApplicationDbContext();
            var review = new Review
            {
                Nombre = userName,
                Comentario = comentario,
                Calificacion = calificacion,
                Fecha = DateTime.Now,
                Mostrar = false,
                Servicio = appointment.ServiceName
            };
            db.Reviews.Add(review);

            // Otorgar sello solo si no lo tiene ya
            if (!db.Sellos.Any(s => s.ReservaId == appointment.Id && s.UserId == userName))
            {
                var sello = new Sello
                {
                    UserId = userName,
                    Servicio = appointment.ServiceName,
                    FechaObtencion = DateTime.Now,
                    ReservaId = appointment.Id,
                    UsadoEnPase = false
                };
                db.Sellos.Add(sello);
            }

            db.SaveChanges();

            TempData["Mensaje"] = "¡Gracias por tu reseña! Has ganado un sello.";
            return RedirectToAction("ReviewUser", new { reservaId });
        }

        public ActionResult Gracias()
        {
            return View();
        }
    }
}