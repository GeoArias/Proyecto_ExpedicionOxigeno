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
    public class ReservasController : Controller
    {
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

                    TempData["Success"] = "¡Reserva confirmada con éxito!";
                    // Pasar los datos a la vista de confirmación
                    ViewBag.Servicio = servicio;
                    ViewBag.SlotStart = slotStart;
                    ViewBag.SlotEnd = slotEnd;
                    ViewBag.Booking = appointmentMS;

                    await EnviarCorreoConfirmacion(user.Email, servicio, slotStart, slotEnd, appointmentMS);

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


                // Bloque temporal para depuración: imprime los Ids de reservas pasadas
foreach (var a in userAppointments.Where(x => x.end?.dateTime < DateTime.Now))
{
    System.Diagnostics.Debug.WriteLine($"Reserva pasada: Id={a.Id}, Servicio={a.ServiceName}, End={a.end?.dateTime}");
}

                return View(userAppointments);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar reservas: " + ex.Message;
                return View(new List<Microsoft.Graph.Models.BookingAppointment>());
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


        private async Task EnviarCorreoConfirmacion(string email, BookingService servicio, DateTime slotStart, DateTime slotEnd, BookingAppointment booking)
        {
            try
            {
                // Obtener la URL del logo
                string logoUrl = Url.Content("~/Resources/Images/logo.png");
                if (!logoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    logoUrl = $"{Request.Url.Scheme}://{Request.Url.Authority}{logoUrl}";
                }

                // Crear el cuerpo del correo electrónico usando el mismo diseño que in Confirmacion.cshtml
                string asunto = "Confirmación de tu reserva - Expedición Oxígeno";
                string cuerpo = $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Confirmación de Reserva</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f8f9fa;
            margin: 0;
            padding: 0;
        }}
        
        .reservation-container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        
        .invoice-card {{
            background: white;
            border-radius: 16px;
            box-shadow: 0 10px 40px rgba(0, 0, 0, 0.1);
            overflow: hidden;
            margin-bottom: 30px;
            border: 1px solid #e9ecef;
        }}
        
        .receipt-tear-top, .receipt-tear-bottom {{
            height: 12px;
            background-image: linear-gradient(45deg, white 25%, transparent 25%), linear-gradient(-45deg, white 25%, transparent 25%);
            background-size: 20px 20px;
            background-position: 0 0;
            border-bottom: 1px dashed #ddd;
            margin-bottom: 10px;
        }}
        
        .receipt-tear-bottom {{
            border-top: 1px dashed #ddd;
            border-bottom: none;
            margin-top: 10px;
            margin-bottom: 0;
        }}
        
        .invoice-header {{
            padding: 20px;
            border-bottom: 2px dashed #e9ecef;
            background-color: #f8f9fa;
            text-align: center;
        }}
        
        .invoice-logo {{
            max-height: 60px;
            width: auto;
            margin-bottom: 15px;
        }}
        
        .invoice-body {{
            padding: 20px;
            position: relative;
        }}
        
        .confirmation-stamp {{
            color: rgba(40, 167, 69, 0.7);
            border: 4px solid rgba(40, 167, 69, 0.7);
            padding: 8px 16px;
            border-radius: 8px;
            display: inline-block;
            text-transform: uppercase;
            letter-spacing: 2px;
            font-weight: bold;
            font-size: 18px;
            margin: 0 auto 20px auto;
            display: block;
            width: fit-content;
            transform: rotate(5deg);
        }}
        
        .section-title {{
            border-bottom: 2px solid #f0f0f0;
            margin-bottom: 20px;
            padding-bottom: 10px;
            color: #212529;
            font-size: 18px;
        }}
        
        .invoice-info-card {{
            background-color: #f8f9fa;
            border-radius: 12px;
            padding: 15px;
            margin-bottom: 20px;
            border-left: 4px solid #fa8f23;
        }}
        
        .card-title {{
            font-weight: 600;
            color: #495057;
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 1px solid #e9ecef;
        }}
        
        .invoice-item {{
            display: flex;
            justify-content: space-between;
            padding: 8px 0;
            border-bottom: 1px dashed #e9ecef;
        }}
        
        .item-label {{
            font-weight: 600;
            color: #495057;
        }}
        
        .payment-info {{
            background-color: #f8f9fa;
            border-radius: 12px;
            padding: 15px;
            margin-bottom: 20px;
            border-left: 4px solid #28a745;
        }}
        
        .price-row {{
            display: flex;
            justify-content: space-between;
            padding: 8px 0;
            border-bottom: 1px solid #e9ecef;
        }}
        
        .total {{
            font-weight: 700;
            font-size: 18px;
            color: #212529;
            border-top: 2px solid #e9ecef;
            border-bottom: none !important;
            padding-top: 10px;
        }}
        
        .payment-notice {{
            background-color: #d1ecf1;
            border-radius: 5px;
            padding: 10px;
            margin-top: 15px;
            color: #0c5460;
        }}
        
        .barcode-section {{
            text-align: center;
            margin: 20px 0;
        }}
        
        .important-notes {{
            background-color: #fff8e1;
            border-radius: 12px;
            padding: 15px;
            margin-top: 20px;
            border-left: 4px solid #ffc107;
        }}
        
        .important-notes h5 {{
            color: #ff9800;
            margin-bottom: 10px;
        }}
        
        .important-notes ul {{
            margin: 0;
            padding-left: 20px;
        }}
        
        .important-notes li {{
            margin-bottom: 5px;
            color: #795548;
        }}
        
        .invoice-footer {{
            padding: 15px;
            background-color: #f8f9fa;
            border-top: 2px dashed #e9ecef;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class='reservation-container'>
        <div style='text-align: center; margin-bottom: 20px;'>
            <h1 style='color: #212529;'>Confirmación de Reserva</h1>
            <p style='color: #6c757d;'>¡Tu aventura está a punto de comenzar!</p>
        </div>

        <div class='invoice-card'>
            <!-- Receipt Top Tear -->
            <div class='receipt-tear-top'></div>

            <!-- Receipt Header -->
            <div class='invoice-header'>
                <img src='{logoUrl}' alt='ExpediCheck Logo' class='invoice-logo'>
                <h2 style='margin: 0; color: #212529;'>Expedición Oxígeno</h2>
                <p style='margin: 5px 0 0 0; color: #6c757d;'>Centro Comercial Oxígeno, Heredia</p>
                <p style='margin: 5px 0 0 0; color: #6c757d;'>Fecha: {booking.CreatedDateTime.Value.ToString("dd/MM/yyyy")}</p>
            </div>

            <!-- Receipt Body -->
            <div class='invoice-body'>
                <div class='confirmation-stamp'>CONFIRMADO</div>

                <h3 class='section-title'>
                    <img src='https://cdn-icons-png.flaticon.com/512/1611/1611154.png' alt='✓' style='width: 20px; height: 20px; margin-right: 10px;'>
                    Detalles de la Reserva
                </h3>

                <div class='invoice-info-card'>
                    <div class='card-title'>
                        <img src='https://cdn-icons-png.flaticon.com/512/189/189664.png' alt='i' style='width: 16px; height: 16px; margin-right: 10px;'>
                        Información del Servicio
                    </div>
                    <div class='invoice-item'>
                        <span class='item-label'>Servicio:</span>
                        <span>{servicio.DisplayName}</span>
                    </div>
                    <div class='invoice-item'>
                        <span class='item-label'>Duración:</span>
                        <span>
                            {(servicio.DefaultDuration.Value.TotalHours >= 1 ? $"{Math.Floor(servicio.DefaultDuration.Value.TotalHours)} horas {servicio.DefaultDuration.Value.Minutes} minutos" : $"{servicio.DefaultDuration.Value.Minutes} minutos")}
                        </span>
                    </div>
                   
<div class=""invoice-item"" style=""display: flex; flex-direction: column; align-items: flex-start; border-bottom: 1px dashed #e9ecef; padding: 10px 0;"">
    <span class=""item-label"" style=""font-weight: 600; margin-bottom: 5px;"">Descripción:</span>
    <span style=""color: #6c757d; font-style: italic; white-space: pre-line; word-break: break-word;"">{servicio.Description}</span>
</div>

                </div>

                <div class='invoice-info-card'>
                    <div class='card-title'>
                        <img src='https://cdn-icons-png.flaticon.com/512/747/747310.png' alt='calendar' style='width: 16px; height: 16px; margin-right: 10px;'>
                        Fecha y Hora
                    </div>
                    <div class='invoice-item'>
                        <span class='item-label'>Fecha:</span>
                        <span>{char.ToUpper(slotStart.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"))[0]) + slotStart.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES")).Substring(1)}</span>
                    </div>
                    <div class='invoice-item'>
                        <span class='item-label'>Horario:</span>
                        <span>{slotStart.ToString("hh:mm tt")} - {slotEnd.ToString("hh:mm tt")}</span>
                    </div>
                </div>

                <div class='payment-info'>
                    <div class='card-title'>
                        <img src='https://cdn-icons-png.flaticon.com/512/991/991952.png' alt='money' style='width: 16px; height: 16px; margin-right: 10px;'>
                        Información de Pago
                    </div>
                    <div>
                        <div class='price-row'>
                            <span>Subtotal:</span>
                            <span>₡{string.Format("{0:N0}", servicio.DefaultPrice * 0.87)}</span>
                        </div>
                        <div class='price-row'>
                            <span>IVA (13%):</span>
                            <span>₡{string.Format("{0:N0}", servicio.DefaultPrice * 0.13)}</span>
                        </div>
                        <div class='price-row total'>
                            <span>Total:</span>
                            <span>₡{string.Format("{0:N0}", servicio.DefaultPrice)}</span>
                        </div>
                    </div>
                    <div class='payment-notice'>
                        <strong>Información de pago:</strong> El pago se realiza directamente en las cajas de Expedición Oxígeno.
                    </div>
                </div>

                <div class='important-notes'>
                    <h5>Información Importante</h5>
                    <ul>
                        <li>Preséntate 15 minutos antes de tu horario reservado.</li>
                        <li>Usa ropa cómoda y zapatos cerrados.</li>
                        <li>Sigue todas las instrucciones de seguridad del personal.</li>
                        <li>En caso de cancelación, comunícate con al menos 24 horas de anticipación.</li>
                    </ul>
                </div>
            </div>

            <!-- Receipt Footer -->
            <div class='invoice-footer'>
                <p style='margin: 0;'>¡Gracias por tu reserva! Esperamos verte pronto.</p>
                <p style='margin: 5px 0 0 0; font-size: 12px; color: #6c757d;'>Para cualquier consulta, llama al 2520-2100</p>
            </div>

            <!-- Receipt Bottom Tear -->
            <div class='receipt-tear-bottom'></div>
        </div>
    </div>
</body>
</html>";

                // Enviar el correo electrónico usando el EmailService
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
                // Registrar el error pero no afectar el flujo principal
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
            var reservas = await MSBookings_Actions.GetAppointmentsByEmail(user.Email);
            var reserva = reservas.FirstOrDefault(r => r.Id == id);

            if (reserva == null)
            {
                TempData["Error"] = "Reserva no encontrada.";
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
            var fechaSeleccionada = fecha ?? reserva.start.dateTime.Date;

            // Obtener staff asignado al servicio
            var staffIds = servicio.StaffMemberIds;
            string userTimeZone = "Central America Standard Time";
            var staffAvailability = await MSBookings_Actions.Get_MSBookingsStaffAvailability(
                staffIds, fechaSeleccionada, fechaSeleccionada.AddDays(1), userTimeZone);

            // Generar slots disponibles
            var availableSlots = await GenerateAvailableTimeSlotsAsync(staffAvailability, servicio.DefaultDuration.Value, fechaSeleccionada, servicio);

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
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(nuevaHoraInicio) || string.IsNullOrEmpty(nuevaHoraFin))
            {
                TempData["Error"] = "Todos los campos son obligatorios.";
                return RedirectToAction("MisReservas");
            }

            try
            {
                //var appointment = await MSBookings_Actions.Get_MSBookingsAppointment(id);

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
                // Ejecutar la modificación usando customerName, no customerId

                var result = await MSBookings_Actions.Modify_MSBookingsAppointment(appointment);

                
                if (result.IsSuccessStatusCode)
                {
                    // Obtener el appointment actualizado para enviar el correo de confirmación
                    var appointmentUpdated = await MSBookings_Actions.Get_MSBookingsAppointment(id);
                    // Obtener el servicio de la reserva
                    var servicio = await MSBookings_Actions.Get_MSBookingsService(appointmentUpdated.ServiceId);

                    TempData["Mensaje"] = "Reserva modificada correctamente.";
                    await EnviarCorreoConfirmacion(id,servicio, fechaHoraInicio, fechaHoraFin, appointment);
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