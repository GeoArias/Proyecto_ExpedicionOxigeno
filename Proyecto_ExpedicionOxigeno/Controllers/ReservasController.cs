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
            // Cargar todos los servicios disponibles
            List<BookingService> services = await MSBookings_Actions.Get_MSBookingsServices();
            ViewBag.Services = services;
            BookingStaffAvailabilityCollectionResponse listaDisponibilidad = new BookingStaffAvailabilityCollectionResponse();
            // Por cada servicio, obtener la disponibilidad del personal
            foreach (var service in services)
            {
                BookingStaffAvailabilityCollectionResponse staffAvailability = await MSBookings_Actions.Get_MSBookingsStaffAvailability(service.StaffMemberIds, DateTime.Today, DateTime.UtcNow.AddDays(30));
                ViewBag.StaffAvailability = staffAvailability;
            }

            return View();
        }
    }
}