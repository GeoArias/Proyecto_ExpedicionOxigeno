using System.Threading.Tasks;
using System.Web.Mvc;
using Proyecto_ExpedicionOxigeno.Helpers;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    [Authorize]
    public class BookingsServiceController : Controller
    {
        private string GetServicesUrl(string businessId) =>
            $"https://graph.microsoft.com/v1.0/solutions/bookingBusinesses/{businessId}/services";

        // GET: BookingsService?businessId={businessId}
        public async Task<ActionResult> Index(string businessId)
        {
            var response = await GraphApiHelper.SendGraphRequestAsync(GetServicesUrl(businessId), HttpMethod.Get);
            var content = await response.Content.ReadAsStringAsync();
            var services = JObject.Parse(content)["value"];
            return View(services);
        }

        // Implement Details, Create, Edit, Delete similar to BookingsBusinessController
    }
}