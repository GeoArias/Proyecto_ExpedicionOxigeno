using Microsoft.Graph.Models;
using Newtonsoft.Json.Linq;
using Proyecto_ExpedicionOxigeno.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    public class ReservasController : Controller
    {
        // GET: Reservas
        public async Task<ActionResult> Index()
        {
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
                var availableSlots = await GenerateAvailableTimeSlotsAsync(staffAvailability, servicio.DefaultDuration.Value, fecha.Value);

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
        public async Task<ActionResult> ElegirHorario(string serviceId, string slotStart, string slotEnd)
        {
            if (string.IsNullOrEmpty(serviceId) || string.IsNullOrEmpty(slotStart) || string.IsNullOrEmpty(slotEnd))
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

                // Parsear fechas
                DateTime startTime, endTime;
                if (!DateTime.TryParse(slotStart, out startTime) || !DateTime.TryParse(slotEnd, out endTime))
                {
                    TempData["Error"] = "Formato de fecha inválido.";
                    return RedirectToAction("SeleccionarServicio", new { id = serviceId, fecha = DateTime.Today });
                }

                // Pasar los datos a la vista de confirmación
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
        public async Task<ActionResult> ConfirmarReserva(string serviceId, DateTime slotStart, DateTime slotEnd, string nombre, string email, string telefono)
        {
            if (string.IsNullOrEmpty(serviceId) || string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(telefono))
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

                // Seleccionar un staff al azar si hay varios disponibles
                Random rnd = new Random();
                string selectedStaffId = availableStaff[rnd.Next(availableStaff.Count)];

                // Aquí implementarías la lógica para crear la reserva en MS Bookings
                // Esto dependerá de cómo está estructurado tu método para crear reservas

                TempData["Success"] = "¡Reserva confirmada con éxito!";
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
            List<TimeSlot> availableSlots = new List<TimeSlot>();

             try
            {
                System.Diagnostics.Debug.WriteLine("Starting GenerateAvailableTimeSlotsAsync method");
                
                if (staffAvailability?.Value == null)
                    return availableSlots;

                // Obtener el servicio actual del ViewBag (asumimos que se ha pasado a este método)
                BookingService servicio = ViewBag.Servicio as BookingService;
                
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