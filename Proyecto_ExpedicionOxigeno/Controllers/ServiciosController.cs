using Newtonsoft.Json.Linq;
using Proyecto_ExpedicionOxigeno.Helpers;
using Proyecto_ExpedicionOxigeno.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
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
            if(id != null)
            {
                try
                {
                    var service = await  MSBookings_Actions.Get_MSBookingsService(id);
                    if (service == null)
                    {
                        return HttpNotFound();
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

        // POST : Servicios/Edit/{ID}
        [HttpPost]
        public async Task<ActionResult> Edit(string id, MSBookings_Service service)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id), "El ID del servicio no puede ser nulo.");
            }
            try
            {
                var response = await MSBookings_Actions.Update_MSBookingsService(id, service);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Servicios");
                }
                else
                {
                    ModelState.AddModelError("", await response.Content.ReadAsStringAsync());
                    return View();
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
    }
}