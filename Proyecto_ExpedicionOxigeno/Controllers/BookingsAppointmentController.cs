using Microsoft.AspNet.Identity;
using Newtonsoft.Json.Linq;
using Proyecto_ExpedicionOxigeno.Helpers;
using Proyecto_ExpedicionOxigeno.Models;
using Proyecto_ExpedicionOxigeno.Services;
using System;
using System.Data.Entity;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    [Authorize]
    public class BookingsAppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SelloService _selloService;

        public BookingsAppointmentController()
        {
            _context = new ApplicationDbContext();
            _selloService = new SelloService(_context);
        }
        private string GetAppointmentsUrl(string businessId) =>
            $"https://graph.microsoft.com/v1.0/solutions/bookingBusinesses/{businessId}/appointments";

        // GET: BookingsAppointment?businessId={businessId}
        public async Task<ActionResult> Index(string businessId)
        {
            var response = await GraphApiHelper.SendGraphRequestAsync(GetAppointmentsUrl(businessId), HttpMethod.Get);
            var content = await response.Content.ReadAsStringAsync();
            var appointments = JObject.Parse(content)["value"];
            return View(appointments);
        }


        // Método para generar QR después de confirmar reserva
        [HttpPost]
        public async Task<ActionResult> GenerarQRParaReserva(string reservaId, string servicio)
        {
            var userId = User.Identity.GetUserId();

            try
            {
                var codigoQR = await _selloService.GenerarCodigoQRAsync(userId, reservaId, servicio);

                return Json(new
                {
                    exito = true,
                    codigoQR = codigoQR,
                    mensaje = "Código QR generado exitosamente"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    exito = false,
                    mensaje = "Error al generar código QR: " + ex.Message
                });
            }
        }

        // Obtener QR de una reserva específica
        public async Task<ActionResult> ObtenerQRReserva(string reservaId)
        {
            var userId = User.Identity.GetUserId();

            var qr = await _context.CodigosQR
                .FirstOrDefaultAsync(q => q.UserId == userId && q.ReservaId == reservaId);

            if (qr == null)
            {
                return HttpNotFound("Código QR no encontrado");
            }

            var imagen = _selloService.GenerarImagenQR(qr.Codigo);
            return File(imagen, "image/png");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
        // Implement Details, Create, Edit, Delete similar to BookingsBusinessController
    }
}