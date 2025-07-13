using Microsoft.Graph.Models;
using Proyecto_ExpedicionOxigeno.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

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
                var services = await MSBookings_Actions.Get_MSBookingsServices();
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
                var servicio = await MSBookings_Actions.Get_MSBookingsService(id);
                if (servicio == null)
                {
                    TempData["Error"] = "El servicio seleccionado no existe.";
                    return RedirectToAction("Index");
                }

                var staffIds = servicio.StaffMemberIds;
                if (staffIds == null || !staffIds.Any())
                {
                    TempData["Error"] = "Este servicio no tiene personal asignado.";
                    return RedirectToAction("Index");
                }

                var startDate = fecha.Value.Date;
                var endDate = startDate.AddDays(1).AddSeconds(-1);

                string userTimeZone = Request.Browser.IsMobileDevice ?
                    TimeZoneInfo.Local.Id :
                    System.Web.HttpContext.Current.Request.Headers["TimeZone"] ?? TimeZoneInfo.Local.Id;

                var staffAvailability = await MSBookings_Actions.Get_MSBookingsStaffAvailability(
                    staffIds, startDate, endDate, userTimeZone);

                ViewBag.Servicio = servicio;
                var availableSlots = await GenerateAvailableTimeSlotsAsync(staffAvailability, servicio.DefaultDuration.Value, fecha.Value);

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
        public async Task<ActionResult> ElegirHorario(string serviceId, string slotStart, string slotEnd)
        {
            if (string.IsNullOrEmpty(serviceId) || string.IsNullOrEmpty(slotStart) || string.IsNullOrEmpty(slotEnd))
            {
                TempData["Error"] = "Información incompleta para la reserva.";
                return RedirectToAction("Index");
            }

            try
            {
                var servicio = await MSBookings_Actions.Get_MSBookingsService(serviceId);
                if (servicio == null)
                {
                    TempData["Error"] = "El servicio seleccionado no existe.";
                    return RedirectToAction("Index");
                }

                if (!DateTime.TryParse(slotStart, out var startTime) || !DateTime.TryParse(slotEnd, out var endTime))
                {
                    TempData["Error"] = "Formato de fecha inválido.";
                    return RedirectToAction("SeleccionarServicio", new { id = serviceId, fecha = DateTime.Today });
                }

                ViewBag.Servicio = servicio;
                ViewBag.SlotStart = startTime;
                ViewBag.SlotEnd = endTime;

                return View("ConfirmarHorario");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar la solicitud: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Reservas/ConfirmarReserva
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmarReserva(string serviceId, DateTime slotStart, DateTime slotEnd)
        {
            if (string.IsNullOrEmpty(serviceId))
            {
                TempData["Error"] = "Todos los campos son obligatorios.";
                return RedirectToAction("Index");
            }

            try
            {
                var servicio = await MSBookings_Actions.Get_MSBookingsService(serviceId);

                var staffIds = servicio.StaffMemberIds;
                string userTimeZone = Request.Browser.IsMobileDevice ?
                    TimeZoneInfo.Local.Id :
                    System.Web.HttpContext.Current.Request.Headers["TimeZone"] ?? TimeZoneInfo.Local.Id;

                var staffAvailability = await MSBookings_Actions.Get_MSBookingsStaffAvailability(
                    staffIds, slotStart.Date, slotStart.Date.AddDays(1), userTimeZone);

                var availableStaff = FindAvailableStaffForSlot(staffAvailability, slotStart, slotEnd);

                if (!availableStaff.Any())
                {
                    TempData["Error"] = "No hay personal disponible para el horario seleccionado.";
                    return RedirectToAction("SeleccionarServicio", new { id = serviceId, fecha = slotStart.Date });
                }

                var userId = User.Identity.GetUserId();
                using (var db = new ApplicationDbContext())
                {
                    var usuario = db.Users.Find(userId);
                    if (usuario == null)
                    {
                        TempData["Error"] = "No se pudo obtener la información del usuario.";
                        return RedirectToAction("Index");
                    }

                    // Guardar la reserva usando HoraInicio y HoraFin
                    var reserva = new Reserva
                    {
                        Usuario = usuario.Email,
                        Nombre = usuario.Nombre,
                        TipoActividad = servicio.DisplayName,
                        DiaReserva = slotStart.Date,
                        HoraInicio = slotStart,
                        HoraFin = slotEnd
                    };
                    db.Reservas.Add(reserva);
                    await db.SaveChangesAsync();

                    var emailService = new EmailService();
                    await emailService.SendAsync(new Microsoft.AspNet.Identity.IdentityMessage
                    {
                        Destination = usuario.Email,
                        Subject = "Confirmación de reserva - Expedición Oxígeno",
                        Body = $@"
<div style='font-family: Arial, sans-serif; max-width: 520px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 10px; padding: 32px; background: #f8fafc;'>
    <div style='text-align:center; margin-bottom:24px;'>
        <img src='https://expedicionoxigeno.com/logo.png' alt='Expedición Oxígeno' style='max-width:160px; height:auto;' />
    </div>
    <h2 style='color: #27ae60; margin-bottom: 16px;'>¡Reserva confirmada!</h2>
    <p style='font-size: 17px; color: #222; margin-bottom: 18px;'>Hola <strong>{usuario.Nombre}</strong>,</p>
    <p style='font-size: 16px; color: #333; margin-bottom: 18px;'>
        Tu reserva para <strong>{servicio.DisplayName}</strong> ha sido confirmada.
    </p>
    <div style='background: #eafaf1; border-radius: 8px; padding: 18px; margin-bottom: 18px;'>
        <ul style='list-style:none; padding-left:0; font-size:16px; color:#222;'>
            <li><strong>Fecha:</strong> {slotStart:dd/MM/yyyy}</li>
            <li><strong>Horario:</strong> {slotStart:HH:mm} - {slotEnd:HH:mm}</li>
        </ul>
    </div>
    <div style='background: #fffbe6; border-radius: 8px; padding: 16px; margin-bottom: 18px; border: 1px solid #ffe58f;'>
        <strong style='color: #d35400;'>Importante:</strong>
        <p style='margin: 8px 0 0 0; color: #d35400; font-size: 15px;'>
            Por favor preséntate <strong>15 minutos antes</strong> al lugar para realizar el pago en el counter de Oxígeno.
        </p>
    </div>
    <p style='font-size: 15px; color: #555; margin-bottom: 18px;'>
        Te esperamos en <strong>Expedición Oxígeno</strong>. Si necesitas modificar o cancelar tu reserva, contáctanos.
    </p>
    <hr style='margin: 24px 0; border: none; border-top: 1px solid #e0e0e0;' />
    <p style='font-size: 13px; color: #aaa; text-align:center;'>
        Este correo es una confirmación automática. No respondas a este mensaje.
    </p>
</div>"
                    });
                }

                TempData["Success"] = "¡Reserva confirmada con éxito! Te llegará un correo electrónico con los detalles de la reserva.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar la reserva: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Método auxiliar para generar slots disponibles según duración del servicio
        private async Task<List<TimeSlot>> GenerateAvailableTimeSlotsAsync(BookingStaffAvailabilityCollectionResponse staffAvailability, TimeSpan serviceDuration, DateTime selectedDate)
        {
            var availableSlots = new List<TimeSlot>();

            try
            {
                System.Diagnostics.Debug.WriteLine("Starting GenerateAvailableTimeSlotsAsync method");

                if (staffAvailability?.Value == null)
                    return availableSlots;

                var servicio = ViewBag.Servicio as BookingService;
                var preBuffer = servicio.PreBuffer ?? TimeSpan.Zero;
                var postBuffer = servicio.PostBuffer ?? TimeSpan.Zero;
                var totalDuration = preBuffer + serviceDuration + postBuffer;

                // 1. Obtener reservas existentes para ese servicio y día
                List<Reserva> reservasExistentes;
                using (var db = new ApplicationDbContext())
                {
                    reservasExistentes = db.Reservas
                        .Where(r => r.TipoActividad == servicio.DisplayName && r.DiaReserva == selectedDate.Date)
                        .ToList();
                }

                // 2. Filtrar solo los items para la fecha seleccionada
                var dateAvailabilityItems = new List<BookingStaffAvailabilityItems>();
                foreach (var staff in staffAvailability.Value)
                {
                    if (staff.AvailabilityItems == null)
                        continue;

                    foreach (var item in staff.AvailabilityItems)
                    {
                        if (item.Status != "available")
                            continue;

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

                var business = await MSBookings_Actions.Get_MSBookingsBusiness();
                string dayOfWeek = selectedDate.DayOfWeek.ToString().ToLower();
                var dayHours = business.BusinessHours?.FirstOrDefault(h => h.Day.ToString().ToLower() == dayOfWeek);

                var slotStart = selectedDate.Date.AddHours(9);
                var slotEnd = selectedDate.Date.AddHours(17);

                if (dayHours != null && dayHours.TimeSlots != null && dayHours.TimeSlots.Any())
                {
                    var timeSlot = dayHours.TimeSlots.First();
                    var startTime = TimeSpan.Parse(timeSlot.StartTime.ToString());
                    var endTime = TimeSpan.Parse(timeSlot.EndTime.ToString());
                    slotStart = selectedDate.Date.Add(startTime);
                    slotEnd = selectedDate.Date.Add(endTime);
                }

                while (slotStart.Add(totalDuration) <= slotEnd)
                {
                    var actualStart = slotStart.Add(preBuffer);
                    var actualEnd = actualStart.Add(serviceDuration);
                    var bufferEnd = actualEnd.Add(postBuffer);

                    bool isSlotAvailable = IsStaffAvailableForSlot(dateAvailabilityItems, slotStart, bufferEnd);

                    // 3. Verificar si el slot está ocupado por alguna reserva existente
                    bool slotOcupado = reservasExistentes.Any(r =>
                        actualStart < r.HoraFin && actualEnd > r.HoraInicio
                    );

                    if (isSlotAvailable && !slotOcupado)
                    {
                        availableSlots.Add(new TimeSlot
                        {
                            StartTime = actualStart,
                            EndTime = actualEnd,
                            BufferStartTime = slotStart,
                            BufferEndTime = bufferEnd
                        });
                    }

                    slotStart = slotStart.AddMinutes(30);
                }

                System.Diagnostics.Debug.WriteLine($"Finished generating {availableSlots.Count} available slots");
                return availableSlots;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GenerateAvailableTimeSlotsAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        // Verificar si hay personal disponible para un slot específico (incluyendo buffers)
        private bool IsStaffAvailableForSlot(List<BookingStaffAvailabilityItems> availabilityItems, DateTime bufferStart, DateTime bufferEnd)
        {
            if (availabilityItems == null || !availabilityItems.Any())
                return false;
            var bookingAvailabilityItems = availabilityItems.Select(a => a.bookingAvailabilityItem).ToList();

            foreach (var staff in bookingAvailabilityItems)
            {
                DateTime staffStart = staff.StartDateTime.DateTime;
                DateTime staffEnd = staff.EndDateTime.DateTime;

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

                    DateTime staffStart = item.StartDateTime.DateTime;
                    DateTime staffEnd = item.EndDateTime.DateTime;

                    if (staffStart <= start && staffEnd >= end)
                    {
                        availableStaff.Add(staff.StaffId);
                        break;
                    }
                }
            }

            return availableStaff;
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