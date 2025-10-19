using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Proyecto_ExpedicionOxigeno.Models;
using System;
using System.IdentityModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        
        /*                  Disabled
        // GET: Reviews/
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para realizar reseñas.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
                var user = await userManager.FindByIdAsync(User.Identity.GetUserId());

                var userAppointments = await MSBookings_Actions.GetAppointmentsByEmail(user.Email);

                // Check which appointments already have reviews via Sellos
                string userId = User.Identity.GetUserId(); // Extract user ID first

                // Extract appointment IDs into a simple collection first
                var appointmentIds = userAppointments.Select(a => a.Id).ToList();

                // Now use the simple collection in the database query
                var ReviewedReservationIds = db.Sellos
                    .Where(r => appointmentIds.Contains(r.ReservaId)).ToList();

                // Add info to ViewBag
                ViewBag.ReviewedReservationIds = ReviewedReservationIds;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar las reservas: " + ex.Message;
                return View();
            }
        }
        */

        // GET: Reviews/New?reservaId=XXXXX
        [HttpGet]
        public async Task<ActionResult> New(string reservaId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para realizar reseñas.";
                return RedirectToAction("Login", "Account");
            }
            var userEmail = User.Identity.Name;
            var appointments = await MSBookings_Actions.GetAppointmentsByEmail(userEmail);

            var appointment = appointments
                .FirstOrDefault(a => a.Id == reservaId && a.end?.dateTime < DateTime.Now);

            if (appointment == null)
            {
                TempData["Error"] = "No se encontró la reserva. Por favor selecciona de nuevo.";
                return RedirectToAction("MisReservas", "Reservas");
            }

            ViewBag.ServiceName = appointment.ServiceName;
            return View(appointment);
        }

        // POST: Reviews/New?reservaId=XXXXX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> New(string reservaId, int calificacion, string comentario)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para realizar reseñas.";
                return RedirectToAction("Login", "Account");
            }
            if(reservaId == null || calificacion<1 || calificacion>5)
            {
                return View();
            }
            try
            {
                var user = db.Users.Find(User.Identity.GetUserId());

                var userEmail = user.Email;
                var userName = user.Nombre;
                var userId = user.Id;

                var appointments = await MSBookings_Actions.GetAppointmentsByEmail(userEmail);
                var appointment = appointments
                    .FirstOrDefault(a => a.Id == reservaId && a.end?.dateTime < DateTime.Now);

                if (appointment == null)
                {
                    TempData["Error"] = "No se encontró una reserva pasada con ese identificador.";
                    return RedirectToAction("New", new { reservaId = reservaId });
                }

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
                return RedirectToAction("MisReservas","Reservas");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al guardar la reseña: " + ex.Message;
                return RedirectToAction("New", new { reservaId });
            }
        }

        public ActionResult Gracias()
        {
            return View();
        }
    }
}