using System.IO;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using Proyecto_ExpedicionOxigeno.Helpers;
using Proyecto_ExpedicionOxigeno.Services;
using Proyecto_ExpedicionOxigeno.Models;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    public class WebhookController : Controller
    {
        private readonly BookingsQRHelper _bookingsHelper;

        public WebhookController()
        {
            var context = new ApplicationDbContext();
            var selloService = new SelloService(context);
            _bookingsHelper = new BookingsQRHelper(selloService, context);
        }

        [HttpPost]
        public async Task<ActionResult> BookingsWebhook()
        {
            try
            {
                using (var reader = new StreamReader(Request.InputStream))
                {
                    var body = await reader.ReadToEndAsync();
                    var webhookData = JObject.Parse(body);

                    var procesado = await _bookingsHelper.ProcesarWebhookBookingsAsync(webhookData);

                    if (procesado)
                    {
                        return new HttpStatusCodeResult(200, "OK");
                    }
                    else
                    {
                        return new HttpStatusCodeResult(500, "Error processing webhook");
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en webhook: {ex.Message}");
                return new HttpStatusCodeResult(500, "Internal server error");
            }
        }

        // Endpoint para validar webhook (MS Bookings verification)
        [HttpGet]
        public ActionResult BookingsWebhook(string validationToken)
        {
            // Devolver el token de validación para verificar el webhook
            return Content(validationToken, "text/plain");
        }
    }
}