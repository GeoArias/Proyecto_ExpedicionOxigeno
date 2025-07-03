using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class MSBookings_Appointment
    {
        [Key]
        public Guid Id { get; set; }
        
        public string CustomerTimeZone { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmailAddress { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerNotes { get; set; }
        public bool SmsNotificationsEnabled { get; set; }
        
        public virtual DateTimeTimeZone Start { get; set; }
        public virtual DateTimeTimeZone End { get; set; }
        
        public bool IsCustomerAllowedToManageBooking { get; set; }
        public bool IsLocationOnline { get; set; }
        public bool OptOutOfCustomerEmail { get; set; }
        public string AnonymousJoinWebUrl { get; set; }
        public string PostBuffer { get; set; }
        public string PreBuffer { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        public BookingPriceType PriceType { get; set; }
        
        public virtual ICollection<BookingReminder> Reminders { get; set; }
        
        public Guid ServiceId { get; set; }
        public virtual Location ServiceLocation { get; set; }
        public string ServiceName { get; set; }
        public string ServiceNotes { get; set; }
        
        public virtual ICollection<string> StaffMemberIds { get; set; }
        
        public int MaximumAttendeesCount { get; set; }
        public int FilledAttendeesCount { get; set; }
        
        public virtual ICollection<BookingCustomerInformation> Customers { get; set; }
    }
    
    public class DateTimeTimeZone
    {
        [Key]
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public string TimeZone { get; set; }
    }
    
    public enum BookingPriceType
    {
        Undefined,
        FixedPrice,
        StartingAt,
        HourlyRate,
        Free,
        PriceVaries
    }
    
    public class BookingReminder
    {
        [Key]
        public int Id { get; set; }
        public string Message { get; set; }
        public string Offset { get; set; }
        public BookingReminderRecipients Recipients { get; set; }
    }
    
    public enum BookingReminderRecipients
    {
        AllAttendees,
        Staff,
        Customer
    }
    
    public class Location
    {
        [Key]
        public int Id { get; set; }
        public virtual PhysicalAddress Address { get; set; }
        public virtual GeoCoordinates Coordinates { get; set; }
        public string DisplayName { get; set; }
        public string LocationEmailAddress { get; set; }
        public LocationType? LocationType { get; set; }
        public string LocationUri { get; set; }
        public string UniqueId { get; set; }
        public LocationUniqueIdType? UniqueIdType { get; set; }
    }
    
    public class PhysicalAddress
    {
        [Key]
        public int Id { get; set; }
        public string City { get; set; }
        public string CountryOrRegion { get; set; }
        public string PostalCode { get; set; }
        public string PostOfficeBox { get; set; }
        public string State { get; set; }
        public string Street { get; set; }
        public PhysicalAddressType? Type { get; set; }
    }
    
    public class GeoCoordinates
    {
        [Key]
        public int Id { get; set; }
        public double? Altitude { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Accuracy { get; set; }
        public double? AltitudeAccuracy { get; set; }
    }
    
    public enum PhysicalAddressType
    {
        Unknown,
        Home,
        Business,
        Other
    }
    
    public enum LocationType
    {
        Default,
        ConferenceRoom,
        HomeAddress,
        BusinessAddress,
        GeoCoordinates,
        StreetAddress,
        Hotel,
        Restaurant,
        LocalBusiness,
        PostalAddress
    }
    
    public enum LocationUniqueIdType
    {
        Unknown,
        LocationStore,
        Directory,
        Private,
        Bing
    }
    
    public class BookingCustomerInformation
    {
        [Key]
        public int Id { get; set; }
        public Guid CustomerId { get; set; }
        public string Name { get; set; }
        public string EmailAddress { get; set; }
        public string Phone { get; set; }
        public string Notes { get; set; }
        public virtual Location Location { get; set; }
        public string TimeZone { get; set; }
        public virtual ICollection<BookingQuestionAnswer> CustomQuestionAnswers { get; set; }
    }
    
    public class BookingQuestionAnswer
    {
        [Key]
        public int Id { get; set; }
        public Guid QuestionId { get; set; }
        public string Question { get; set; }
        public string AnswerInputType { get; set; }
        public virtual ICollection<string> AnswerOptions { get; set; }
        public bool IsRequired { get; set; }
        public string Answer { get; set; }
        public virtual ICollection<string> SelectedOptions { get; set; }    
    }
}