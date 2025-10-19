using Azure;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Proyecto_ExpedicionOxigeno.Helpers;
using Proyecto_ExpedicionOxigeno.Models;
using Proyecto_ExpedicionOxigeno.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using ZXing;
using ZXing.Common;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    [Authorize]
    public class ReservasController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        // GET: Reservas
        public async Task<ActionResult> Index()
        {
            //Si el usuario njo está autenticado, redirigir a la página de Inicio de Sesión
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para realizar reservas.";
                return RedirectToAction("Login", "Account");
            }
            try
            {
                // Cargar todos los servicios disponibles
                List<BookingService> services = await MSBookings_Actions.Get_MSBookingsServices();
                ViewBag.Services = services;

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar servicios: {ex.Message}";
                return View();
            }
        }

        // GET: Reservas/SeleccionarServicio
        public async Task<ActionResult> SeleccionarServicio(string id, DateTime? fecha)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para realizar reservas.";
                return RedirectToAction("Login", "Account");
            }
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Debes seleccionar un servicio.";
                return RedirectToAction("Index");
            }

            if (!fecha.HasValue)
            {
                fecha = DateTime.Today;
            }

            // Validar que la fecha no sea anterior a hoy
            if (fecha.Value.Date < DateTime.Today)
            {
                fecha = DateTime.Today;
            }

            try
            {
                // Obtener detalles del servicio seleccionado
                BookingService servicio = await MSBookings_Actions.Get_MSBookingsService(id);
                if (servicio == null)
                {
                    TempData["Error"] = "El servicio seleccionado no existe.";
                    return RedirectToAction("Index");
                }

                // Obtener personal asociado a este servicio
                List<string> staffIds = servicio.StaffMemberIds;
                if (staffIds == null || !staffIds.Any())
                {
                    TempData["Error"] = "Este servicio no tiene personal asignado.";
                    return RedirectToAction("Index");
                }

                // Obtener disponibilidad del personal para el día seleccionado
                var startDate = fecha.Value.Date;
                var endDate = startDate.AddDays(1).AddSeconds(-1); // Hasta el final del día seleccionado

                // Get the user's timezone (this could be from a user profile or browser detection)
                string userTimeZone = Request.Browser.IsMobileDevice ?
                    TimeZoneInfo.Local.Id : // Simplified - you might need a better way to get mobile timezone
                    System.Web.HttpContext.Current.Request.Headers["TimeZone"] ?? TimeZoneInfo.Local.Id;

                var staffAvailability = await MSBookings_Actions.Get_MSBookingsStaffAvailability(
                    staffIds, startDate, endDate, userTimeZone);

                // Generar slots disponibles según la duración del servicio
                ViewBag.Servicio = servicio;
                var availableSlots = await GenerateAvailableTimeSlotsAsync(staffAvailability, servicio.DefaultDuration.Value, fecha.Value, servicio);


                // Excluir horario ya pasados
                availableSlots = availableSlots.Where(slot => slot.EndTime >= DateTime.Now).ToList();

                // Verificar si sí hay slots, sino devolver al usuario a la página anterior
                if (availableSlots.Count == 0)
                {
                    TempData["Error"] = "No hay horarios disponibles para la fecha seleccionada";
                    return RedirectToAction("Index");
                }
                // Pasar datos a la vista
                ViewBag.AvailableSlots = availableSlots;
                ViewBag.FechaSeleccionada = fecha.Value;

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar la solicitud: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Reservas/ElegirHorario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ElegirHorario(string serviceId, DateTime slotStart, DateTime slotEnd)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para realizar reservas.";
                return RedirectToAction("Login", "Account");
            }
            if (string.IsNullOrEmpty(serviceId) || slotStart == null || slotEnd == null)
            {
                TempData["Error"] = "Información incompleta para la reserva.";
                return RedirectToAction("Index");
            }

            try
            {
                // Obtener detalles del servicio
                var servicio = await MSBookings_Actions.Get_MSBookingsService(serviceId);
                if (servicio == null)
                {
                    TempData["Error"] = "El servicio seleccionado no existe.";
                    return RedirectToAction("Index");
                }


                // Pasar los datos a la vista de confirmación
                ViewBag.Servicio = servicio;
                ViewBag.SlotStart = slotStart;
                ViewBag.SlotEnd = slotEnd;

                return View("ConfirmarHorario");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar la solicitud: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: Reservas/ConfirmarHorario
        [HttpGet]
        public ActionResult ConfirmarHorario()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para realizar reservas.";
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // POST: Reservas/ConfirmarReserva
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmarReserva(string serviceId, DateTime slotStart, DateTime slotEnd)
        {

            // Validar que el usuario esté autenticado
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para realizar reservas.";
                return RedirectToAction("Login", "Account");
            }
            if (string.IsNullOrEmpty(serviceId))
            {
                TempData["Error"] = "Todos los campos son obligatorios.";
                return RedirectToAction("Index");
            }

            try
            {
                // Obtener detalles del servicio
                var servicio = await MSBookings_Actions.Get_MSBookingsService(serviceId);

                // Obtener staff disponible para esa franja
                var staffIds = servicio.StaffMemberIds;

                // Get the user's timezone (this could be from a user profile or browser detection)
                string userTimeZone = Request.Browser.IsMobileDevice ?
                    TimeZoneInfo.Local.Id : // Simplified - you might need a better way to get mobile timezone
                    System.Web.HttpContext.Current.Request.Headers["TimeZone"] ?? TimeZoneInfo.Local.Id;

                var staffAvailability = await MSBookings_Actions.Get_MSBookingsStaffAvailability(
                    staffIds, slotStart.Date, slotStart.Date.AddDays(1), userTimeZone);

                // Filtrar personal disponible para el slot seleccionado
                var availableStaff = FindAvailableStaffForSlot(staffAvailability, slotStart, slotEnd);

                if (!availableStaff.Any())
                {
                    TempData["Error"] = "No hay personal disponible para el horario seleccionado.";
                    return RedirectToAction("SeleccionarServicio", new { id = serviceId, fecha = slotStart.Date });
                }

                // Obtener el usuario logueado actualmente
                ApplicationUserManager _userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
                var user = await _userManager.FindByIdAsync(User.Identity.GetUserId());


                // Seleccionar un staff al azar si hay varios disponibles
                Random rnd = new Random();
                string selectedStaffId = availableStaff[rnd.Next(availableStaff.Count)];

                // Crear la reserva en MS Bookings
                HttpResponseMessage appointment = await MSBookings_Actions.Create_MSBookingsAppointment(
                    serviceId,
                    selectedStaffId,
                    slotStart,
                    slotEnd,
                    user.Nombre,
                    user.Email,
                    user.Telefono
                );



                if (appointment.IsSuccessStatusCode)
                {
                    // Obtener la reserva creada
                    var settings = new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> {
                            new GraphTimeSpanConverter(),
                            new GraphTimeConverter()
                        },
                        NullValueHandling = NullValueHandling.Ignore,
                    };
                    var responseContent = await appointment.Content.ReadAsStringAsync();

                    BookingAppointment appointmentMS = JObject.Parse(responseContent).ToObject<BookingAppointment>(
                        JsonSerializer.Create(settings));

                    // Generar el código QR único para la reserva
                    var qrHelper = new BookingsQRHelper(new SelloService(new ApplicationDbContext()), new ApplicationDbContext());
                    string codigoQR = await qrHelper.GenerarQRParaReservaAsync(JObject.Parse(responseContent), user.Id);

                    TempData["Success"] = "¡Reserva confirmada con éxito!";
                    ViewBag.Servicio = servicio;
                    ViewBag.SlotStart = slotStart;
                    ViewBag.SlotEnd = slotEnd;
                    ViewBag.Booking = appointmentMS;

                    // Enviar el correo con el QR
                    await EnviarCorreoConfirmacion(user.Email, servicio, slotStart, slotEnd, appointmentMS, codigoQR);

                    return View("Confirmacion");
                }
                else
                {
                    TempData["Error"] = "Ha ocurrido un error: " + appointment.ReasonPhrase;
                    return RedirectToAction("Index");
                }


            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar la reserva: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: Reservas/MisReservas
        public async Task<ActionResult> MisReservas()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión.";
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


                return View(userAppointments);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar reservas: " + ex.Message;
                return View(new List<Microsoft.Graph.Models.BookingAppointment>());
            }
        }

        // GET: Reservas/VerReservas
        public async Task<ActionResult> VerReservas()
        {
            //Si el usuario njo está autenticado, redirigir a la página de Inicio de Sesión
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para realizar reservas.";
                return RedirectToAction("Login", "Account");
            }
            try
            {
                // Cargar todos los servicios disponibles
                List<BookingService> services = await MSBookings_Actions.Get_MSBookingsServices();
                ViewBag.Services = services;

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar servicios: {ex.Message}";
                return View();
            }
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelarReserva(string id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para cancelar reservas.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["Error"] = "ID de reserva no válido.";
                    return RedirectToAction("MisReservas");
                }
                var reserva = await MSBookings_Actions.Get_MSBookingsAppointment(id);

                if (reserva == null)
                {
                    TempData["Error"] = "Reserva no encontrada.";
                    return RedirectToAction("MisReservas");
                }
                if (reserva.CustomerEmailAddress.ToLower().Trim() != User.Identity.Name)
                {
                    // El usuario no es el dueño de la reserva
                    TempData["Error"] = "No tienes permiso para cancelar esta reserva.";
                    return RedirectToAction("MisReservas");
                }

                var response = await MSBookings_Actions.Cancel_MSBookingsAppointment(id);

                if (response.IsSuccessStatusCode)
                {
                    // También actualizar el sello relacionado si existe
                    var db = new ApplicationDbContext();
                    var sello = db.Sellos.FirstOrDefault(s => s.ReservaId == id);
                    if (sello != null)
                    {
                        // Marcar el sello como no válido o eliminarlo
                        db.Sellos.Remove(sello);
                        db.SaveChanges();
                    }

                    TempData["Success"] = "Reserva cancelada correctamente.";
                }
                else
                {
                    TempData["Error"] = $"Error al cancelar la reserva: {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cancelar la reserva: {ex.Message}";
            }

            return RedirectToAction("MisReservas");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ModificarReserva(string id, DateTime nuevaFecha, DateTime nuevaHoraInicio, DateTime nuevaHoraFin)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    TempData["Error"] = "Debes iniciar sesión para modificar reservas.";
                    return RedirectToAction("Login", "Account");
                }
                // Obtener el objeto de reserva actual
                var reservas = await MSBookings_Actions.Get_MSBookingsAppointment(id);
                // await MSBookings_Actions.Modify_MSBookingsAppointment(id);
                TempData["Mensaje"] = "Reserva modificada correctamente.";
                return RedirectToAction("MisReservas");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al modificar la reserva: " + ex.Message;

                // ✅ Intentamos reconstruir el modelo si ocurrió error
                var reservas = await MSBookings_Actions.GetAppointmentsByEmail(User.Identity.Name);
                var reserva = reservas.FirstOrDefault(r => r.Id == id);

                if (reserva == null)
                    return RedirectToAction("MisReservas");

                // Necesitás esto para que no reviente la vista:
                ViewBag.AvailableSlots = new List<TimeSlot>(); // O los slots correctos si querés
                ViewBag.FechaSeleccionada = nuevaFecha;

                return View("Editar", reserva);
            }
        }

        // Método auxiliar para generar slots disponibles según duración del servicio
        private async Task<List<TimeSlot>> GenerateAvailableTimeSlotsAsync(
    BookingStaffAvailabilityCollectionResponse staffAvailability,
    TimeSpan serviceDuration,
    DateTime selectedDate,
    BookingService servicio)
        {
            List<TimeSlot> availableSlots = new List<TimeSlot>();

            try
            {
                System.Diagnostics.Debug.WriteLine("Starting GenerateAvailableTimeSlotsAsync method");

                if (staffAvailability?.Value == null)
                    return availableSlots;

                // Determinar los tiempos de buffer (usar 0 si son nulos)
                TimeSpan preBuffer = servicio.PreBuffer ?? TimeSpan.Zero;
                TimeSpan postBuffer = servicio.PostBuffer ?? TimeSpan.Zero;

                // Duración total requerida incluyendo buffers
                TimeSpan totalDuration = preBuffer + serviceDuration + postBuffer;

                // Filtrar solo los items para la fecha seleccionada
                List<BookingStaffAvailabilityItems> dateAvailabilityItems = new List<BookingStaffAvailabilityItems>();

                foreach (var staff in staffAvailability.Value)
                {
                    if (staff.AvailabilityItems == null)
                        continue;

                    foreach (var item in staff.AvailabilityItems)
                    {
                        if (item.Status != "available")
                            continue;

                        // Solo considerar elementos para la fecha seleccionada
                        if (item.StartDateTime.DateTime.DayOfYear == selectedDate.Date.DayOfYear)
                        {
                            dateAvailabilityItems.Add(new BookingStaffAvailabilityItems
                            {
                                bookingAvailabilityItem = item,
                                StaffId = staff.StaffId
                            });
                        }
                    }
                }

                // Crear slots para este día (usando el horario del Get_MSBookingsBusiness())
                BookingBusiness business = await MSBookings_Actions.Get_MSBookingsBusiness();

                string dayOfWeek = selectedDate.DayOfWeek.ToString().ToLower();

                // Buscar las horas de negocio para el día específico
                var dayHours = business.BusinessHours?.FirstOrDefault(h => h.Day.ToString().ToLower() == dayOfWeek);

                // Establecer horarios predeterminados en caso de que no haya datos
                DateTime slotStart = selectedDate.Date.AddHours(9); // Predeterminado: 9:00 AM
                DateTime slotEnd = selectedDate.Date.AddHours(17);  // Predeterminado: 5:00 PM

                // Si hay horarios definidos para este día, usarlos
                if (dayHours != null && dayHours.TimeSlots != null && dayHours.TimeSlots.Any())
                {
                    var timeSlot = dayHours.TimeSlots.First();

                    // Parsear las horas del formato "HH:MM:SS.SSSSSSS"
                    TimeSpan startTime = TimeSpan.Parse(timeSlot.StartTime.ToString());
                    TimeSpan endTime = TimeSpan.Parse(timeSlot.EndTime.ToString());

                    slotStart = selectedDate.Date.Add(startTime);
                    slotEnd = selectedDate.Date.Add(endTime);
                }

                while (slotStart.Add(totalDuration) <= slotEnd)
                {
                    // Calcular tiempo real de inicio y fin (incluyendo buffers)
                    var actualStart = slotStart.Add(preBuffer);
                    var actualEnd = actualStart.Add(serviceDuration);
                    var bufferEnd = actualEnd.Add(postBuffer);

                    // Verificar si hay al menos un staff disponible para este slot completo (incluyendo buffers)
                    bool isSlotAvailable = IsStaffAvailableForSlot(dateAvailabilityItems, slotStart, bufferEnd);

                    if (isSlotAvailable)
                    {
                        availableSlots.Add(new TimeSlot
                        {
                            // Guardamos el tiempo visible para el cliente (sin el pre/post buffer)
                            StartTime = actualStart,
                            EndTime = actualEnd,
                            // Podríamos agregar propiedades adicionales para uso interno
                            BufferStartTime = slotStart,
                            BufferEndTime = bufferEnd
                        });
                    }

                    // Avanzar al siguiente slot (incrementos de 30 minutos)
                    slotStart = slotStart.AddMinutes(30);
                }

                System.Diagnostics.Debug.WriteLine($"Finished generating {availableSlots.Count} available slots");
                return availableSlots;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GenerateAvailableTimeSlotsAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to preserve the exception stack
            }
        }

        // Verificar si hay personal disponible para un slot específico (incluyendo buffers)
        private bool IsStaffAvailableForSlot(List<BookingStaffAvailabilityItems> availabilityItems, DateTime bufferStart, DateTime bufferEnd)
        {
            if (availabilityItems == null || !availabilityItems.Any())
                return false;
            List<BookingStaffAvailabilityItem> bookingAvailabilityItems = availabilityItems.Select(a => a.bookingAvailabilityItem).ToList();

            foreach (var staff in bookingAvailabilityItems)
            {
                // Convertir a DateTime local para comparación
                DateTime staffStart = staff.StartDateTime.DateTime;
                DateTime staffEnd = staff.EndDateTime.DateTime;

                // Verificar si este staff está disponible durante todo el slot (incluyendo buffers)
                if (staffStart <= bufferStart && staffEnd >= bufferEnd)
                {
                    return true;
                }
            }

            return false;
        }

        // Encontrar personal disponible para un slot específico
        private List<string> FindAvailableStaffForSlot(BookingStaffAvailabilityCollectionResponse staffAvailability, DateTime start, DateTime end)
        {
            var availableStaff = new List<string>();

            if (staffAvailability?.Value == null)
                return availableStaff;

            foreach (var staff in staffAvailability.Value)
            {
                if (staff.AvailabilityItems == null)
                    continue;

                foreach (var item in staff.AvailabilityItems)
                {
                    if (item.Status != "available")
                        continue;

                    // Convertir a DateTime local para comparación
                    DateTime staffStart = item.StartDateTime.DateTime;
                    DateTime staffEnd = item.EndDateTime.DateTime;

                    // Verificar si este staff está disponible durante todo el slot
                    if (staffStart <= start && staffEnd >= end)
                    {
                        availableStaff.Add(staff.StaffId);
                        break; // Ya encontramos disponibilidad para este staff
                    }
                }
            }

            return availableStaff;
        }


        private async Task EnviarCorreoConfirmacion(string email, BookingService servicio, DateTime slotStart, DateTime slotEnd, BookingAppointment booking, string codigoQR)
        {
            try
            {
                string asunto = "Confirmación de Reserva - Expedición Oxígeno";
                string cuerpo = $@"
<html>
<head>
  <meta charset='utf-8'>
  <title>Confirmación de Reserva - Expedición Oxígeno</title>
</head>
<body style='background:#f8f9fa; margin:0; padding:0; font-family:Arial,sans-serif;'>
  <div style='max-width:600px; margin:40px auto; background:#fff; border-radius:16px; box-shadow:0 10px 40px rgba(0,0,0,0.08); border:1px solid #e9ecef; overflow:hidden;'>
    <div style='background:linear-gradient(135deg,#fa8f23 0%,#ff7e00 100%); padding:32px 24px 24px 24px; text-align:center;'>
      <img src='{Url.Content("~/Resources/Images/logo.png")}' alt='Expedición Oxígeno' style='height:60px; margin-bottom:16px;' />
      <h1 style='color:#fff; font-size:2rem; margin:0;'>Expedición Oxígeno</h1>
      <p style='color:#fff; font-size:1.1rem; margin:8px 0 0 0;'>¡Reserva confirmada!</p>
    </div>
    <div style='padding:32px 24px;'>
      <p style='font-size:1.1rem; color:#333; margin-bottom:24px;'>Gracias por reservar en Expedición Oxígeno. Aquí tienes los detalles de tu reserva:</p>
      <div style='background:#e9f7ef; border-radius:8px; padding:18px 20px; margin-bottom:24px; color:#222; font-size:1rem;'>
        <strong>Servicio:</strong> {servicio.DisplayName}<br/>
        <strong>Fecha:</strong> {slotStart.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"))}<br/>
        <strong>Horario:</strong> {slotStart.ToString("hh:mm tt")} - {slotEnd.ToString("hh:mm tt")}<br/>
      </div>
      <div style='text-align:center; margin:32px 0;'>
        <img src='{Url.Action("GenerarQRReserva", "Reservas", new { codigo = codigoQR }, Request.Url.Scheme)}' alt='Código QR de Reserva' style='max-width:180px; margin-bottom:10px; border-radius:8px; box-shadow:0 2px 8px rgba(0,0,0,0.08);' />
        <div style='color:#6c757d; font-size:13px; margin-top:8px;'>Presenta este código QR al llegar para validar tu reserva y obtener tu sello</div>
      </div>
      <div style='background:#fff8e1; border-radius:8px; padding:18px 20px; margin-bottom:24px; border-left:4px solid #ffc107;'>
        <h3 style='color:#ff9800; margin-bottom:12px; font-size:1.1rem;'>Información Importante</h3>
        <ul style='color:#795548; padding-left:18px; margin:0; font-size:1rem;'>
          <li>Preséntate 15 minutos antes de tu horario reservado.</li>
          <li>Usa ropa cómoda y zapatos cerrados.</li>
          <li>Sigue todas las instrucciones de seguridad del personal.</li>
          <li>En caso de cancelación, comunícate con al menos 24 horas de anticipación.</li>
        </ul>
      </div>
      <div style='text-align:center; margin-top:32px;'>
        <p style='font-size:1.1rem; color:#28a745; margin:0;'>¡Gracias por tu reserva! Esperamos verte pronto.</p>
        <p style='font-size:0.95rem; color:#6c757d; margin:8px 0 0 0;'>Para cualquier consulta, llama al <strong>2520-2100</strong></p>
      </div>
    </div>
  </div>
</body>
</html>";

                var emailService = new EmailService();
                await emailService.SendAsync(new Microsoft.AspNet.Identity.IdentityMessage
                {
                    Destination = email,
                    Subject = asunto,
                    Body = cuerpo
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al enviar correo de confirmación: {ex.Message}");
            }
        }

        // GET: Reservas/Editar/{id}
        public async Task<ActionResult> Editar(string id, DateTime? fecha)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para modificar reservas.";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "ID de reserva no válido.";
                return RedirectToAction("MisReservas");
            }

            var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var user = await userManager.FindByIdAsync(User.Identity.GetUserId());
            var reserva = await MSBookings_Actions.Get_MSBookingsAppointment(id);

            if (reserva == null)
            {
                TempData["Error"] = "Reserva no encontrada.";
                return RedirectToAction("MisReservas");
            }
            if (reserva.CustomerEmailAddress.ToLower().Trim() != User.Identity.Name)
            {
                // El usuario no es el dueño de la reserva
                TempData["Error"] = "No tienes permiso para modificar esta reserva.";
                return RedirectToAction("MisReservas");
            }

            // Obtener el servicio de la reserva
            var servicio = await MSBookings_Actions.Get_MSBookingsService(reserva.ServiceId);
            if (servicio == null)
            {
                TempData["Error"] = "Servicio no encontrado.";
                return RedirectToAction("MisReservas");
            }

            // Determinar la fecha a consultar (por defecto la de la reserva)
            DateTime? fechaSeleccionada = fecha;

if (!fechaSeleccionada.HasValue)
{
    if (reserva != null && reserva.start != null)
    {
        fechaSeleccionada = reserva.start.dateTime.Date;
    }
    else
    {
        // Fallback: use today's date or handle as needed
        fechaSeleccionada = DateTime.Today;
        TempData["Error"] = "No se pudo determinar la fecha de la reserva. Se usará la fecha actual.";
    }
}

            // Obtener staff asignado al servicio
            var staffIds = servicio.StaffMemberIds;
            string userTimeZone = "Central America Standard Time";
            var staffAvailability = await MSBookings_Actions.Get_MSBookingsStaffAvailability(
                staffIds, fechaSeleccionada.Value, fechaSeleccionada.Value.AddDays(1), userTimeZone);

            // Generar slots disponibles
            var availableSlots = await GenerateAvailableTimeSlotsAsync(
                staffAvailability, servicio.DefaultDuration.Value, fechaSeleccionada.Value, servicio);

            ViewBag.Servicio = servicio;
            ViewBag.AvailableSlots = availableSlots;
            ViewBag.FechaSeleccionada = fechaSeleccionada;

            return View(reserva);
        }

        // POST: Reservas/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Editar(string id, DateTime nuevaFecha, string nuevaHoraInicio, string nuevaHoraFin, BookingAppointmentCustomed appointment)
        {
            // Validar que el usuario esté autenticado
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para realizar reservas.";
                return RedirectToAction("Login", "Account");
            }
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(nuevaHoraInicio) || string.IsNullOrEmpty(nuevaHoraFin))
            {
                TempData["Error"] = "Todos los campos son obligatorios.";
                return RedirectToAction("MisReservas");
            }

            try
            {
                
                // Combinar fecha con hora
                DateTime horaInicio = DateTime.ParseExact(nuevaHoraInicio, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                DateTime horaFin = DateTime.ParseExact(nuevaHoraFin, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);

                DateTime fechaHoraInicio = new DateTime(nuevaFecha.Year, nuevaFecha.Month, nuevaFecha.Day, horaInicio.Hour, horaInicio.Minute, 0);
                DateTime fechaHoraFin = new DateTime(nuevaFecha.Year, nuevaFecha.Month, nuevaFecha.Day, horaFin.Hour, horaFin.Minute, 0);

                // Actualizar el objeto appointment con las nuevas fechas
                if (appointment.start == null)
                    appointment.start = new TimesBooking();
                if (appointment.end == null)
                    appointment.end = new TimesBooking();

                appointment.start.dateTime = fechaHoraInicio;
                appointment.start.timeZone = TimeZoneInfo.Local.Id;
                appointment.end.dateTime = fechaHoraFin;
                appointment.end.timeZone = TimeZoneInfo.Local.Id;

                //Verificar que el usuario actual sea el dueño de la reserva y que la reserva exista

                var Previousappointment = await MSBookings_Actions.Get_MSBookingsAppointment(id);
                if (Previousappointment == null)
                {
                    // La reserva no pudo ser encontrada
                    TempData["Error"] = "La reserva no pudo ser encontrada";
                    return RedirectToAction("MisReservas");
                }
                
                if (Previousappointment.CustomerEmailAddress.ToLower().Trim() != User.Identity.Name)
                {
                    // El usuario no es el dueño de la reserva
                    TempData["Error"] = "No tienes permiso para modificar esta reserva.";
                    return RedirectToAction("MisReservas");
                }
                 
                // Ejecutar la modificación usando el ID
                var result = await MSBookings_Actions.Modify_MSBookingsAppointment(appointment);

                
                if (result.IsSuccessStatusCode)
                {
                    // Obtener el appointment actualizado para enviar el correo de confirmación
                    var appointmentUpdated = await MSBookings_Actions.Get_MSBookingsAppointment(id);
                    // Obtener el servicio de la reserva
                    var servicio = await MSBookings_Actions.Get_MSBookingsService(appointmentUpdated.ServiceId);

                    TempData["Mensaje"] = "Reserva modificada correctamente.";

                    // Obtener el código QR de la reserva actualizada
                    string codigoQR = null;
                    var db = new ApplicationDbContext();
                    var sello = db.Sellos.FirstOrDefault(s => s.ReservaId == id);
                    if (sello != null)
                    {
                        codigoQR = sello.CodigoQR;
                    }
                    else
                    {
                        // Si no existe, puedes generar uno nuevo o dejarlo en null
                        codigoQR = Guid.NewGuid().ToString("N");
                    }

                    await EnviarCorreoConfirmacion(
                        Previousappointment.CustomerEmailAddress, // email
                        servicio,
                        fechaHoraInicio,
                        fechaHoraFin,
                        appointment,
                        codigoQR
                    );

                    return RedirectToAction("MisReservas");
                }
                else
                {
                    TempData["Error"] = $"Error al modificar la reserva: {result.ReasonPhrase}";

                    // Recuperar la reserva para recargar la vista en caso de error
                    var emailUsuario = User.Identity.Name;
                    var reservas = await MSBookings_Actions.GetAppointmentsByEmail(emailUsuario);
                    var reserva = reservas.FirstOrDefault(r => r.Id == id);

                    if (reserva == null)
                        return RedirectToAction("MisReservas");

                    ViewBag.FechaSeleccionada = nuevaFecha;
                    ViewBag.AvailableSlots = new List<TimeSlot>(); // Opcionalmente: recalcular si querés

                    return View("Editar", reserva);
                }

                
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al modificar la reserva: {ex.Message}";

                // Recuperar la reserva para recargar la vista en caso de error
                var emailUsuario = User.Identity.Name;
                var reservas = await MSBookings_Actions.GetAppointmentsByEmail(emailUsuario);
                var reserva = reservas.FirstOrDefault(r => r.Id == id);

                if (reserva == null)
                    return RedirectToAction("MisReservas");

                ViewBag.FechaSeleccionada = nuevaFecha;
                ViewBag.AvailableSlots = new List<TimeSlot>(); // Opcionalmente: recalcular si querés

                return View("Editar", reserva);
            }
        }
        // GET: Reservas/GenerarQRReserva
        [HttpGet]
        public ActionResult GenerarQRReserva(string codigo)
        {
            try
            {
                if (string.IsNullOrEmpty(codigo))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Código no proporcionado");
                }

                var imagen = GenerarImagenQR(codigo);
                return File(imagen, "image/png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generando QR: {ex.Message}");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Error al generar código QR");
            }
        }
        public byte[] GenerarImagenQR(string codigo, int width = 300, int height = 300)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = height,
                    Width = width,
                    Margin = 1
                }
            };

            using (var bitmap = writer.Write(codigo))
            {
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
        }
    }


        // Clase auxiliar para representar un slot de tiempo disponible
        public class TimeSlot
    {
        public DateTime StartTime { get; set; }  // Hora visible de inicio (después del preBuffer)
        public DateTime EndTime { get; set; }    // Hora visible de fin (antes del postBuffer)

        // Campos adicionales para manejo interno de buffers
        public DateTime BufferStartTime { get; set; }  // Hora real de inicio incluyendo preBuffer
        public DateTime BufferEndTime { get; set; }    // Hora real de fin incluyendo postBuffer
    }


}