using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class BookingAppointmentCustomed: BookingAppointment
    {
        public TimesBooking start { get; set; }
        public TimesBooking end { get; set; }
    }
    public class TimesBooking
    {
        public string timeZone { get; set; }
        public DateTime dateTime { get; set; }
    }
}