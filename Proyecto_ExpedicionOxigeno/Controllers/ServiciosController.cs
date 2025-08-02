using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    public class ServiciosController : Controller
    {


        // GET: Servicios
        public async Task<ActionResult> Index()
        {
            try
            {
                return View(await MSBookings_Actions.Get_MSBookingsServices());
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error de conexión al servidor: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado: {ex.Message}", ex);
            }
        }

        // Get: Servicios/Edit/{ID}
        public async Task<ActionResult> Edit(string id)
        {
            if (id != null)
            {
                try
                {
                    var service = await MSBookings_Actions.Get_MSBookingsService(id);
                    if (service == null)
                    {
                        return HttpNotFound();
                    }
                    // Get all staff members
                    var staffList = await MSBookings_Actions.Get_MSBookingsStaffs();
                    staffList = staffList.FindAll(s => s != null && !string.IsNullOrEmpty(s.Id) && !string.IsNullOrEmpty(s.DisplayName));
                    ViewBag.AllStaff = staffList;

                    return View(service);
                }
                catch (HttpRequestException ex)
                {
                    throw new Exception($"Error de conexión al servidor: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error inesperado: {ex.Message}", ex);
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(id), "El ID del servicio no puede ser nulo.");
            }

        }

        // POST : Servicios/Edit/{ID}
        [HttpPost]
        public async Task<ActionResult> Edit(string id,BookingService service,string[] selectedStaff,int DurationHours = 0,int DurationMinutes = 0,int PreBufferHours = 0,int PreBufferMinutes = 0,int PostBufferHours = 0,int PostBufferMinutes = 0)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id), "El ID del servicio no puede ser nulo.");
            }
            try
            {
                // Actualizar Staff
                service.StaffMemberIds = selectedStaff?.ToList() ?? new List<string>();

                // Actualizar Duraciones
                service.DefaultDuration = new TimeSpan(DurationHours, DurationMinutes, 0);
                service.PreBuffer = new TimeSpan(PreBufferHours, PreBufferMinutes, 0);
                service.PostBuffer = new TimeSpan(PostBufferHours, PostBufferMinutes, 0);

               
                var response = await MSBookings_Actions.Update_MSBookingsService(id, service);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "Servicio modificado satisfactoriamente!";
                    return RedirectToAction("Index");
                }
                else
                {
                    var Nservice = await MSBookings_Actions.Get_MSBookingsService(id);
                    if (Nservice == null)
                    {
                        return HttpNotFound();
                    }
                    // Get all staff members
                    var staffList = await MSBookings_Actions.Get_MSBookingsStaffs();
                    staffList = staffList.FindAll(s => s != null && !string.IsNullOrEmpty(s.Id) && !string.IsNullOrEmpty(s.DisplayName));
                    ViewBag.AllStaff = staffList;
                    TempData["Error"] = "Error al procesar el cambio: " + response.ReasonPhrase;
                    return View(Nservice);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error de conexión al servidor: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado: {ex.Message}", ex);
            }
        }

        // Get: Servicios/CambioHorario/{ID}
        public async Task<ActionResult> CambioHorario(string id)
        {
            if (id != null)
            {
                try
                {
                    var service = await MSBookings_Actions.Get_MSBookingsService(id);
                    if (service == null)
                    {
                        return HttpNotFound();
                    }
                    // Get all staff members
                    var staffList = await MSBookings_Actions.Get_MSBookingsStaffs();
                    staffList = staffList.FindAll(s => s != null && !string.IsNullOrEmpty(s.Id) && !string.IsNullOrEmpty(s.DisplayName));
                    ViewBag.AllStaff = staffList;

                    return View(service);
                }
                catch (HttpRequestException ex)
                {
                    throw new Exception($"Error de conexión al servidor: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error inesperado: {ex.Message}", ex);
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(id), "El ID del servicio no puede ser nulo.");
            }

        }

        // POST: Servicios/CambioHorario/{ID}
        [HttpPost]
        public async Task<ActionResult> CambioHorario(string id, BookingService service, string[] selectedStaff, int DurationHours = 0, int DurationMinutes = 0, int PreBufferHours = 0, int PreBufferMinutes = 0, int PostBufferHours = 0, int PostBufferMinutes = 0)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id), "El ID del servicio no puede ser nulo.");
            }
            try
            {
                // Actualizar Staff
                service.StaffMemberIds = selectedStaff?.ToList() ?? new List<string>();
                // Actualizar customAvailability


                /*
                 // Actualizar disponibilidades personalizadas
                if (service.SchedulingPolicy == null)
                {
                    service.SchedulingPolicy = new BookingSchedulingPolicy();
                }

                // Lista de días de la semana como strings en minúsculas
                List<string> daysOfWeek = new List<string>
                {
                    "monday",
                    "tuesday",
                    "wednesday",
                    "thursday",
                    "friday",
                    "saturday",
                    "sunday"
                };

                if (CustomAvailabilities != null)
                {
                    foreach (var availability in CustomAvailabilities)
                    {
                        var businessHours = new List<BookingWorkHours>();
                        foreach (string day in daysOfWeek)
                        {
                            businessHours.Add(new BookingWorkHours
                            {
                                Day = (DayOfWeekObject)Enum.Parse(typeof(DayOfWeekObject),
                                char.ToUpper(day[0]) + day.Substring(1)), // "monday" -> DayOfWeekObject.Monday
                                TimeSlots = null
                            });
                        }
                        availability.BusinessHours = businessHours;
                    }
                }






                service.SchedulingPolicy.CustomAvailabilities = CustomAvailabilities ?? new List<BookingsAvailabilityWindow>();

                 */

                var response = await MSBookings_Actions.Update_MSBookingsService(id, service);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "Horario modificado satisfactoriamente!";
                    return RedirectToAction("Index");
                }
                else
                {
                    var Nservice = await MSBookings_Actions.Get_MSBookingsService(id);
                    if (Nservice == null)
                    {
                        return HttpNotFound();
                    }
                    // Get all staff members
                    var staffList = await MSBookings_Actions.Get_MSBookingsStaffs();
                    staffList = staffList.FindAll(s => s != null && !string.IsNullOrEmpty(s.Id) && !string.IsNullOrEmpty(s.DisplayName));
                    ViewBag.AllStaff = staffList;
                    TempData["Error"] = "Error al procesar el cambio: " + response.ReasonPhrase;
                    return View(Nservice);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error de conexión al servidor: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado: {ex.Message}", ex);
            }
        }



        // GET: Servicios/Details/{ID}
        public async Task<ActionResult> Details(string id)
        {
            if (id != null)
            {
                try
                {
                    var service = await MSBookings_Actions.Get_MSBookingsService(id);
                    if (service == null)
                    {
                        TempData["Error"] = "Servicio no encontrado";
                        RedirectToAction("Index");
                    }
                    return View(service);
                }
                catch (HttpRequestException ex)
                {
                    throw new Exception($"Error de conexión al servidor: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error inesperado: {ex.Message}", ex);
                }
            }
            else
            {

                throw new ArgumentNullException(nameof(id), "El ID del servicio no puede ser nulo.");
            }
        }
    }
}