using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using System.Xml;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class MSBookings_Service
    {
        [Key]
        public Guid Id { get; set; }


        [Display(Name = "Nombre")]
        public string DisplayName { get; set; }

        [Display(Name = "Duración por defecto")]
        public string DefaultDuration { get; set; }
        
        [NotMapped]
        public TimeSpan DefaultDurationTimeSpan
        {
            get => XmlConvert.ToTimeSpan(DefaultDuration);
            // Convertir de TimeSpan a String
            set => DefaultDuration = XmlConvert.ToString(value);
        }

    

        public double DefaultPrice { get; set; }

        // Tiene que ser siempre "fixedPrice"
        public string DefaultPriceType { get; set; }

        public string Description { get; set; }

        public string LanguageTag { get; set; }

        public bool IsHiddenFromCustomers { get; set; }

        public string Notes { get; set; }

        [Display(Name = "Tiempo antes del servicio")]
        public string PreBuffer { get; set; }

        [NotMapped]
        public TimeSpan PreBufferTimeSpan
        {
            get => XmlConvert.ToTimeSpan(PreBuffer);
            // Convertir de TimeSpan a String
            set => PreBuffer = XmlConvert.ToString(value);
        }

        [Display(Name = "Tiempo después del servicio")]
        public string PostBuffer { get; set; }
        [NotMapped]
        public TimeSpan PostBufferTimeSpan
        {
            get => XmlConvert.ToTimeSpan(PostBuffer);
            set => PostBuffer = XmlConvert.ToString(value);
        }

        public List<string> StaffMemberIds { get; set; }

        public bool IsLocationOnline { get; set; }

        public bool SmsNotificationsEnabled { get; set; }

        public bool IsAnonymousJoinEnabled { get; set; }

        public string WebUrl { get; set; }

        public virtual MSBookings_SchedulingPolicy SchedulingPolicy { get; set; }

        public virtual MSBookings_Location DefaultLocation { get; set; }

        public List<string> DefaultReminders { get; set; }

        public MSBookings_Service()
        {
            StaffMemberIds = new List<string>();
            DefaultReminders = new List<string>();
        }

    }

    public class MSBookings_Location
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Service")]
        public string ServiceId { get; set; }

        public string DisplayName { get; set; }

        public string LocationEmailAddress { get; set; }

        public string LocationUri { get; set; }

        public string LocationType { get; set; }

        public string UniqueId { get; set; }

        public string UniqueIdType { get; set; }

        public virtual MSBookings_Address Address { get; set; }

        public virtual MSBookings_Coordinates Coordinates { get; set; }

        public virtual MSBookings_Service Service { get; set; }
    }


    public class MSBookings_Coordinates
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Location")]
        public int LocationId { get; set; }

        public double? Altitude { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public double? Accuracy { get; set; }

        public double? AltitudeAccuracy { get; set; }

        public virtual MSBookings_Location Location { get; set; }
    }
}