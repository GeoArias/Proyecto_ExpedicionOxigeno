using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Proyecto_ExpedicionOxigeno.Helpers;
using Proyecto_ExpedicionOxigeno.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    public static class MSBookings_Actions
    {
        private static string businessId = ConfigurationManager.AppSettings["ida:MSFTBookingsBusiness"];
        private static string GetServicesUrl(string businessId) =>
            $"https://graph.microsoft.com/v1.0/solutions/bookingBusinesses/{businessId}/services";

        private static BookingBusiness bookingBusiness;

        //
        // Microsoft Bookings: Business
        //
        public static async Task<BookingBusiness> Get_MSBookingsBusiness()
        {
            try
            {
                if (bookingBusiness == null)
                {
                    var response = await GraphApiHelper.SendGraphRequestAsync($"https://graph.microsoft.com/v1.0/solutions/bookingBusinesses/{businessId}", HttpMethod.Get);
                    if (response.IsSuccessStatusCode)
                    {

                    }
                    var content = await response.Content.ReadAsStringAsync();
                    var settings = new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> {
                            new GraphTimeSpanConverter(),
                            new GraphTimeConverter()
                        },
                        NullValueHandling = NullValueHandling.Ignore
                    };
                    
                    bookingBusiness = JObject.Parse(content).ToObject<BookingBusiness>(
                        JsonSerializer.Create(settings));
                }
                return bookingBusiness;
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


        //
        //  Microsoft Bookings: Servicios
        //

        // GET: Servicios
        public static async Task<List<BookingService>> Get_MSBookingsServices()
        {
            try
            {
                var response = await GraphApiHelper.SendGraphRequestAsync(GetServicesUrl(businessId), HttpMethod.Get);

                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content;
                    var jsonString = await content.ReadAsStringAsync();
                    // Parse the JSON string to a JArray
                    var jsonObject = JObject.Parse(jsonString);
                    var servicesArray = jsonObject["value"] as JArray;

                    // Convert JArray to List<BookingService> with our custom settings
                    var settings = new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> { 
                            new GraphTimeSpanConverter(), 
                            new GraphTimeConverter() 
                        },
                        NullValueHandling = NullValueHandling.Ignore
                    };
                    
                    List<BookingService> servicesList = servicesArray.ToObject<List<BookingService>>(
                        JsonSerializer.Create(settings));
                    return servicesList;
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
        public static async Task<BookingService> Get_MSBookingsService(string serviceId)
        {
            try
            {
                var response = await GraphApiHelper.SendGraphRequestAsync($"{GetServicesUrl(businessId)}/{serviceId}", HttpMethod.Get);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var settings = new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> { 
                            new GraphTimeSpanConverter(), 
                            new GraphTimeConverter() 
                        },
                        NullValueHandling = NullValueHandling.Ignore
                    };
                    return JObject.Parse(content).ToObject<BookingService>(
                        JsonSerializer.Create(settings));
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
        public static async Task<HttpResponseMessage> Update_MSBookingsService(string serviceId, BookingService service)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    Converters = new List<JsonConverter> { 
                        new GraphTimeSpanConverter(), 
                        new GraphTimeConverter() 
                    },
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonData = JsonConvert.SerializeObject(service, settings);
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



        //
        //  Microsoft Bookings: Staff/People
        //
        public static async Task<List<BookingStaffMember>> Get_MSBookingsStaff()
        {
            try
            {
                var response = await GraphApiHelper.SendGraphRequestAsync($"https://graph.microsoft.com/v1.0/solutions/bookingBusinesses/{businessId}/staffMembers", HttpMethod.Get);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(content);
                    var staffArray = jsonObject["value"] as JArray;
                    // Convert JArray to List<BookingStaffMember>
                    var staff = staffArray.ToObject<List<BookingStaffMember>>();
                    return staff;
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



        //
        //  Microsoft Bookings: Staff/Availability
        //      https://graph.microsoft.com/v1.0/solutions/bookingBusinesses/Contosolunchdelivery@contoso.com/getStaffAvailability
        //
        public static async Task<BookingStaffAvailabilityCollectionResponse> Get_MSBookingsStaffAvailability(
            List<string> staffIds, DateTime startDate, DateTime endDate, string timeZone = null)
        {
            try
            {
                // Validate input parameters
                if (staffIds == null || !staffIds.Any())
                {
                    throw new ArgumentException("Staff IDs list cannot be empty or null");
                }

                // Use user's timezone if provided, otherwise fall back to server timezone
                timeZone = timeZone ?? TimeZoneInfo.Local.Id;

                string url = $"https://graph.microsoft.com/v1.0/solutions/bookingBusinesses/{businessId}/getStaffAvailability";

                // Format dates properly for Graph API but maintain the user's timezone context
                string startDateFormatted = startDate.ToString("yyyy-MM-ddTHH:mm:ss");
                string endDateFormatted = endDate.ToString("yyyy-MM-ddTHH:mm:ss");

                // Create request object with the user's timezone
                var requestObject = new
                {
                    staffIds = staffIds,
                    startDateTime = new
                    {
                        dateTime = startDateFormatted,
                        timeZone = timeZone
                    },
                    endDateTime = new
                    {
                        dateTime = endDateFormatted,
                        timeZone = timeZone
                    }
                };

                // Serialize with indented formatting for better debugging
                string jsonContent = JsonConvert.SerializeObject(requestObject, Formatting.Indented);
                
                // Create the request content
                HttpContent requestBody = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                // Log or debug the request content
                System.Diagnostics.Debug.WriteLine($"Request JSON: {jsonContent}");

                // Send the request with the serialized JSON content
                var response = await GraphApiHelper.SendGraphRequestAsync(url, HttpMethod.Post, requestBody);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    // Log the response for debugging
                    System.Diagnostics.Debug.WriteLine($"Response: {content}");
                    
                    // Deserialize the entire JSON response directly to BookingStaffAvailabilityCollectionResponse
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    };
                    
                    var result = JsonConvert.DeserializeObject<BookingStaffAvailabilityCollectionResponse>(
                        content, settings);
                        
                    return result;
                }
                else
                {
                    // Log error response
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Error response: {errorContent}");
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


    }
}
