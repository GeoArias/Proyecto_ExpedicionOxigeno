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
    public static class MSBookings_Actions
    {
        private static string businessId = ConfigurationManager.AppSettings["ida:MSFTBookingsBusiness"];
        private static string GetServicesUrl(string businessId) =>
            $"https://graph.microsoft.com/v1.0/solutions/bookingBusinesses/{businessId}/services";

        // GET: Servicios
        public static async Task<List<Models.MSBookings_Service>> Get_MSBookingsServices()
        {
            try
            {
                var response = await GraphApiHelper.SendGraphRequestAsync(GetServicesUrl(businessId), HttpMethod.Get);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(content);
                    var servicesArray = jsonObject["value"] as JArray;

                    // Convert JArray to List<MSBookings_Service>
                    var services = servicesArray.ToObject<List<Models.MSBookings_Service>>();
                    return services;
                }
                else
                {
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al realizar la solicitud HTTP: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado: {ex.Message}", ex);
            }
        }

        // Get: Servicio/ID
        public static async Task<Models.MSBookings_Service> Get_MSBookingsService(string serviceId)
        {
            try
            {
                var response = await GraphApiHelper.SendGraphRequestAsync($"{GetServicesUrl(businessId)}/{serviceId}", HttpMethod.Get);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JObject.Parse(content).ToObject<Models.MSBookings_Service>();
                }
                else
                {
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al realizar la solicitud HTTP: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado: {ex.Message}", ex);
            }
        }

        // POST: Servicios/ID
        public static async Task<HttpResponseMessage> Update_MSBookingsService(string serviceId, MSBookings_Service service)
        {
            try
            {
                string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(service);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
                var response = await GraphApiHelper.SendGraphRequestAsync($"{GetServicesUrl(businessId)}/{serviceId}", HttpMethod.Post, content);
                return response;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al realizar la solicitud HTTP: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado: {ex.Message}", ex);
            }
        }
    }
}