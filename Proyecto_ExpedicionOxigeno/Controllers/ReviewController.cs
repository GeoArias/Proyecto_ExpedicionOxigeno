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
        [HttpGet]
        public async Task<ActionResult> ReviewUser(string reservaId)
        {
            var userEmail = User.Identity.Name;
            var appointments = await MSBookings_Actions.GetAppointmentsByEmail(userEmail);

            var appointment = appointments
                .FirstOrDefault(a => a.Id == reservaId && a.end?.dateTime < DateTime.Now);

            if (appointment == null)
            {
                ViewBag.Error = "No se encontró una reserva pasada con ese identificador.";
                return View();
            }

            ViewBag.ServiceName = appointment.ServiceName;
            ViewBag.ReservaId = reservaId;
            return View();
        }

        // GET: Review/ReviewUser?reservaId=XXXXX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReviewUser(string reservaId, int calificacion, string comentario)
        {
            try
            {
                var userEmail = User.Identity.Name;
                var userName = User.Identity.GetUserName();
                var userId = User.Identity.GetUserId();

                var claimsIdentity = User.Identity as System.Security.Claims.ClaimsIdentity;
                var claimNombre = claimsIdentity?.FindFirst("Nombre");
                var nombre = claimNombre?.Value ?? userName ?? "Anónimo";

                var appointments = await MSBookings_Actions.GetAppointmentsByEmail(userEmail); // <-- Usa await aquí
                var appointment = appointments
                    .FirstOrDefault(a => a.Id == reservaId && a.end?.dateTime < DateTime.Now);

                if (appointment == null)
                {
                    TempData["Error"] = "No se encontró una reserva pasada con ese identificador.";
                    return RedirectToAction("ReviewUser", new { reservaId });
                }

                var db = new ApplicationDbContext();
                var review = new Review
                {
                    Nombre = nombre,
                    Comentario = comentario,
                    Calificacion = calificacion,
                    Fecha = DateTime.Now,
                    Mostrar = false,
                    Servicio = appointment.ServiceName
                };
                db.Reviews.Add(review);

                // Otorgar sello solo si no lo tiene ya
                if (!db.Sellos.Any(s => s.ReservaId == appointment.Id && s.UserId == userId))
                {
                    var sello = new Sello
                    {
                        UserId = userId,
                        Servicio = appointment.ServiceName,
                        FechaObtencion = DateTime.Now,
                        ReservaId = appointment.Id,
                        UsadoEnPase = false,
                        CodigoQR = Guid.NewGuid().ToString("N")
                    };
                    db.Sellos.Add(sello);
                }

                db.SaveChanges();

                TempData["Mensaje"] = "¡Gracias por tu reseña! Has ganado un sello.";
                return RedirectToAction("ReviewUser", new { reservaId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al guardar la reseña: " + ex.Message;
                return RedirectToAction("ReviewUser", new { reservaId });
            }
        }

        public ActionResult Gracias()
        {
            return View();
        }
    }
}