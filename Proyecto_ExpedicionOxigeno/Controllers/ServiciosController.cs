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
        public async Task<ActionResult> Edit(string id, BookingService service, string[] selectedStaff)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id), "El ID del servicio no puede ser nulo.");
            }
            try
            {
                // Update staffMemberIds from the form
                service.StaffMemberIds = selectedStaff?.ToList() ?? new List<string>();

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
                    // Repopulate staff list for redisplay
                    var staffList = await MSBookings_Actions.Get_MSBookingsStaffs();
                    ViewBag.AllStaff = new SelectList(staffList, "Id", "DisplayName");
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