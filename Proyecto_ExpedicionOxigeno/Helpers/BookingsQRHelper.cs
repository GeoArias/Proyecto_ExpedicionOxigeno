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
                if (appointment == null)
                    throw new ArgumentNullException(nameof(appointment), "El objeto appointment es null.");

                var reservaId = appointment["id"]?.ToString();
                var servicioNombre = appointment["service"]?["displayName"]?.ToString();

                // Fallback: intenta con serviceName si displayName no existe
                if (string.IsNullOrEmpty(servicioNombre))
                    servicioNombre = appointment["serviceName"]?.ToString();

                // Fallback: busca el nombre usando el serviceId si sigue sin existir
                if (string.IsNullOrEmpty(servicioNombre) && appointment["serviceId"] != null)
                {
                    var serviceId = appointment["serviceId"].ToString();
                    // Obtener el nombre del servicio desde la API de MS Bookings
                    var servicioApi = await Proyecto_ExpedicionOxigeno.Controllers.MSBookings_Actions.Get_MSBookingsService(serviceId);
                    if (servicioApi != null)
                        servicioNombre = servicioApi.DisplayName;
                }

                var fechaInicioStr = appointment["start"]?["dateTime"]?.ToString();

                if (string.IsNullOrEmpty(reservaId))
                    throw new Exception("El campo 'id' de la reserva es null o vacío.");
                if (string.IsNullOrEmpty(servicioNombre))
                    throw new Exception("No se pudo determinar el nombre del servicio para la reserva.");
                if (string.IsNullOrEmpty(fechaInicioStr))
                    throw new Exception("El campo 'start.dateTime' de la reserva es null o vacío.");

                var fechaInicio = DateTime.Parse(fechaInicioStr);

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