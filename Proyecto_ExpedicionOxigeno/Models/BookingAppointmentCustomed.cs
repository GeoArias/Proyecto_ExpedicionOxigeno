using Microsoft.Graph.Models;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class BookingAppointmentCustomed : BookingAppointment
    {
       

        [JsonProperty("start")]
      
        public TimesBooking start { get; set; }

        [JsonProperty("end")]
       
        public TimesBooking end { get; set; }
    }
}