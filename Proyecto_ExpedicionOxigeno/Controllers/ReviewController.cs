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

        // GET: Review/ReviewUser?serviceId=XXXXX
        public async Task<ActionResult> ReviewUser(string serviceId)
        {
            var userEmail = User.Identity.Name;

            var appointments = await MSBookings_Actions.GetAppointmentsByEmail(userEmail);

            // --- BLOQUE DE DEPURACIÓN ---
            foreach (var a in appointments)
            {
                System.Diagnostics.Debug.WriteLine($"Appointment: ServiceId={a.ServiceId}, End={a.end?.dateTime}, User={userEmail}");
            }
            System.Diagnostics.Debug.WriteLine($"Received serviceId: {serviceId}");
            // --- FIN BLOQUE DE DEPURACIÓN ---

            var appointment = appointments
                .Where(a => a.ServiceId == serviceId && a.end?.dateTime < DateTime.Now)
                .OrderByDescending(a => a.end.dateTime)
                .FirstOrDefault();

            if (appointment == null)
            {
                ViewBag.Error = "No se encontró una reserva pasada para este servicio.";
                return View("ReviewUser");
            }

            ViewBag.ServiceName = appointment.ServiceName;
            ViewBag.ServiceId = serviceId;
            return View();
        }

        // POST: Review/ReviewUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReviewUser(string serviceId, int calificacion, string comentario)
        {
            var userEmail = User.Identity.Name;
            var userName = User.Identity.GetUserName();

            // Obtener el appointment más reciente (igual que en GET)
            var appointments = MSBookings_Actions.GetAppointmentsByEmail(userEmail).Result;
            var appointment = appointments
                .Where(a => a.ServiceId == serviceId && a.end?.dateTime < DateTime.Now)
                .OrderByDescending(a => a.end.dateTime)
                .FirstOrDefault();

            if (appointment == null)
            {
                TempData["Error"] = "No se encontró una reserva pasada para este servicio.";
                return RedirectToAction("ReviewUser", new { serviceId });
            }

            // Guardar la reseña
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
            db.SaveChanges();

            TempData["Mensaje"] = "¡Gracias por tu reseña!";
            return RedirectToAction("Gracias");
        }

        public ActionResult Gracias()
        {
            return View();
        }
    }
}