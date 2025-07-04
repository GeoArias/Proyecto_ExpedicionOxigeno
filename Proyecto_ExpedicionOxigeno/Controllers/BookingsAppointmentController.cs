using System.Threading.Tasks;
using System.Web.Mvc;
using Proyecto_ExpedicionOxigeno.Helpers;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    [Authorize]
    public class BookingsAppointmentController : Controller
    {
        private string GetAppointmentsUrl(string businessId) =>
            $"https://graph.microsoft.com/v1.0/solutions/bookingBusinesses/{businessId}/appointments";

        // GET: BookingsAppointment?businessId={businessId}
        public async Task<ActionResult> Index(string businessId)
        {
            var response = await GraphApiHelper.SendGraphRequestAsync(GetAppointmentsUrl(businessId), HttpMethod.Get);
            var content = await response.Content.ReadAsStringAsync();
            var appointments = JObject.Parse(content)["value"];
            return View(appointments);
        }

        // Implement Details, Create, Edit, Delete similar to BookingsBusinessController
    }
}