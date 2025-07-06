using Newtonsoft.Json.Linq;
using Proyecto_ExpedicionOxigeno.Helpers;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    [Authorize]
    public class BookingsBusinessController : Controller
    {
        private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0/solutions/bookingBusinesses";

        // GET: BookingsBusiness
        public async Task<ActionResult> Index()
        {
            var response = await GraphApiHelper.SendGraphRequestAsync(GraphBaseUrl, HttpMethod.Get);
            var content = await response.Content.ReadAsStringAsync();
            var businesses = JObject.Parse(content)["value"];
            return View(businesses);
        }

        // GET: BookingsBusiness/Details/{id}
        public async Task<ActionResult> Details(string id)
        {
            var response = await GraphApiHelper.SendGraphRequestAsync($"{GraphBaseUrl}/{id}", HttpMethod.Get);
            var content = await response.Content.ReadAsStringAsync();
            var business = JObject.Parse(content);
            return View(business);
        }

        // GET: BookingsBusiness/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: BookingsBusiness/Create
        [HttpPost]
        public async Task<ActionResult> Create(string jsonData)
        {
            var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
            var response = await GraphApiHelper.SendGraphRequestAsync(GraphBaseUrl, HttpMethod.Post, content);
            if (response.IsSuccessStatusCode)
                return RedirectToAction("Index");
            ModelState.AddModelError("", await response.Content.ReadAsStringAsync());
            return View();
        }

        // GET: BookingsBusiness/Edit/{id}
        public async Task<ActionResult> Edit(string id)
        {
            var response = await GraphApiHelper.SendGraphRequestAsync($"{GraphBaseUrl}/{id}", HttpMethod.Get);
            var content = await response.Content.ReadAsStringAsync();
            var business = JObject.Parse(content);
            return View(business);
        }

        // POST: BookingsBusiness/Edit/{id}
        [HttpPost]
        public async Task<ActionResult> Edit(string id, string jsonData)
        {
            var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
            var response = await GraphApiHelper.SendGraphRequestAsync($"{GraphBaseUrl}/{id}", new HttpMethod("PATCH"), content);
            if (response.IsSuccessStatusCode)
                return RedirectToAction("Index");
            ModelState.AddModelError("", await response.Content.ReadAsStringAsync());
            return View();
        }

        // POST: BookingsBusiness/Delete/{id}
        [HttpPost]
        public async Task<ActionResult> Delete(string id)
        {
            var response = await GraphApiHelper.SendGraphRequestAsync($"{GraphBaseUrl}/{id}", HttpMethod.Delete);
            return RedirectToAction("Index");
        }
    }
}