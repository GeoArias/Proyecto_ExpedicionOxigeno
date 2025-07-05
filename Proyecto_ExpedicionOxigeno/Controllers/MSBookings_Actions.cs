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
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(content);
                    var servicesArray = jsonObject["value"] as JArray;

                    // Configure JsonSerializer with our custom converters
                    var settings = new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> { 
                            new IsoTimeSpanConverter(),
                            new KiotaTimeConverter()
                        }
                    };

                    // Convert JArray to List<BookingService> with our custom settings
                    List<BookingService> servicesList = servicesArray.ToObject<List<BookingService>>(JsonSerializer.Create(settings));
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
                            new IsoTimeSpanConverter(),
                            new KiotaTimeConverter()
                        }
                    };
                    return JObject.Parse(content).ToObject<BookingService>(JsonSerializer.Create(settings));
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
        public static async Task<BookingStaffAvailabilityCollectionResponse> Get_MSBookingsStaffAvailability(List<string> staffIds, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Validate input parameters
                if (staffIds == null || !staffIds.Any())
                {
                    throw new ArgumentException("Staff IDs list cannot be empty or null");
                }

                string url = $"https://graph.microsoft.com/v1.0/solutions/bookingBusinesses/{businessId}/getStaffAvailability";

                // Ensure dates are in UTC and properly formatted for Graph API
                string startDateFormatted = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                string endDateFormatted = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                // Create request object with proper naming
                var requestObject = new
                {
                    staffIds = staffIds,
                    startDateTime = startDateFormatted,
                    endDateTime = endDateFormatted
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
                    
                    var jsonObject = JObject.Parse(content);
                    var availabilityArray = jsonObject["value"] as JArray;

                    // Convert JArray to BookingStaffAvailabilityCollectionResponse
                    var availabilityList = availabilityArray.ToObject<BookingStaffAvailabilityCollectionResponse>();
                    return availabilityList;
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

namespace Proyecto_ExpedicionOxigeno.Helpers
{
    public class KiotaTimeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Time) || objectType == typeof(Time?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            string timeString = reader.Value.ToString();
            
            try
            {
                // Parse time string in format HH:MM:SS
                if (TimeSpan.TryParse(timeString, out TimeSpan timeSpan))
                {
                    return new Time(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                }
                
                // If parsing fails, try with regex to extract components
                var match = Regex.Match(timeString, @"(\d{1,2}):(\d{1,2})(?::(\d{1,2}))?");
                if (match.Success)
                {
                    int hours = int.Parse(match.Groups[1].Value);
                    int minutes = int.Parse(match.Groups[2].Value);
                    int seconds = match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value) 
                        ? int.Parse(match.Groups[3].Value) 
                        : 0;
                    
                    return new Time(hours, minutes, seconds);
                }
                
                throw new JsonSerializationException($"Could not parse time value: {timeString}");
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException($"Error converting value '{timeString}' to Time: {ex.Message}");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            Time time = (Time)value;
            string timeString = $"{time.Hour:D2}:{time.Minute:D2}:{time.Second:D2}";
            writer.WriteValue(timeString);
        }
    }
}