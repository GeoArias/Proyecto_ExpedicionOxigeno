using Newtonsoft.Json.Linq;
using Proyecto_ExpedicionOxigeno.Models;
using Proyecto_ExpedicionOxigeno.Services;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace Proyecto_ExpedicionOxigeno.Helpers
{
    public class BookingsQRHelper
    {
        private readonly SelloService _selloService;
        private readonly ApplicationDbContext _context;

        public BookingsQRHelper(SelloService selloService, ApplicationDbContext context)
        {
            _selloService = selloService;
            _context = context;
        }

        // Generar QR automáticamente cuando se confirme una reserva
        public async Task<string> GenerarQRParaReservaAsync(JToken appointment, string userId)
        {
            try
            {
                var reservaId = appointment["id"].ToString();
                var servicioNombre = appointment["service"]["displayName"].ToString();
                var fechaInicio = DateTime.Parse(appointment["start"]["dateTime"].ToString());

                // Verificar si ya existe un QR para esta reserva
                var qrExistente = await _context.CodigosQR
                    .FirstOrDefaultAsync(q => q.ReservaId == reservaId && q.UserId == userId);

                if (qrExistente != null)
                {
                    return qrExistente.Codigo;
                }

                // Generar nuevo QR
                var codigoQR = await _selloService.GenerarCodigoQRAsync(userId, reservaId, servicioNombre);
                return codigoQR;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al generar QR para reserva: {ex.Message}", ex);
            }
        }

        // Webhook para manejar cambios en las reservas de MS Bookings
        public async Task<bool> ProcesarWebhookBookingsAsync(JToken webhookData)
        {
            try
            {
                var changeType = webhookData["changeType"].ToString();
                var resource = webhookData["resource"].ToString();

                if (changeType == "updated" && resource.Contains("appointments"))
                {
                    // Obtener detalles de la reserva actualizada
                    var appointmentId = ExtractAppointmentId(resource);
                    var appointment = await ObtenerDetallesReservaAsync(appointmentId);

                    if (appointment["status"].ToString() == "confirmed")
                    {
                        // Generar QR si la reserva se confirma
                        var customerEmail = appointment["customer"]["emailAddress"].ToString();
                        var usuario = await _context.Users.FirstOrDefaultAsync(u => u.Email == customerEmail);

                        if (usuario != null)
                        {
                            await GenerarQRParaReservaAsync(appointment, usuario.Id);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log del error
                System.Diagnostics.Debug.WriteLine($"Error en webhook: {ex.Message}");
                return false;
            }
        }

        private string ExtractAppointmentId(string resource)
        {
            // Extraer ID de la URL del resource
            var parts = resource.Split('/');
            return parts[parts.Length - 1];
        }

        private async Task<JToken> ObtenerDetallesReservaAsync(string appointmentId)
        {
            // Usar GraphApiHelper para obtener detalles de la reserva
            var url = $"https://graph.microsoft.com/v1.0/solutions/bookingBusinesses/{{businessId}}/appointments/{appointmentId}";
            var response = await GraphApiHelper.SendGraphRequestAsync(url, System.Net.Http.HttpMethod.Get);
            var content = await response.Content.ReadAsStringAsync();
            return JObject.Parse(content);
        }
    }
}