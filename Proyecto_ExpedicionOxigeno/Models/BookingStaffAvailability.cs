using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

// The main response model
public class BookingStaffAvailabilityCollectionResponse
{
    [JsonPropertyName("@odata.context")]
    public string OdataContext { get; set; }
    
    [JsonPropertyName("value")]
    public List<BookingStaffAvailability> Value { get; set; }
}

// Individual staff availability
public class BookingStaffAvailability
{
    [JsonPropertyName("staffId")]
    public string StaffId { get; set; }
    
    [JsonPropertyName("availabilityItems")]
    public List<BookingStaffAvailabilityItem> AvailabilityItems { get; set; }
}

// Specific time slots
public class BookingStaffAvailabilityItem
{
    [JsonPropertyName("status")]
    public string Status { get; set; }
    
    [JsonPropertyName("serviceId")]
    public string ServiceId { get; set; }
    
    [JsonPropertyName("startDateTime")]
    public DateTimeInfo StartDateTime { get; set; }
    
    [JsonPropertyName("endDateTime")]
    public DateTimeInfo EndDateTime { get; set; }
}

// DateTime information with timezone
public class DateTimeInfo
{
    [JsonPropertyName("dateTime")]
    public DateTime DateTime { get; set; }
    
    [JsonPropertyName("timeZone")]
    public string TimeZone { get; set; }
}

public class BookingStaffAvailabilityItems
{
    public BookingStaffAvailabilityItem bookingAvailabilityItem { get; set; }

    [JsonPropertyName("staffId")]
    public string StaffId { get; set; }

}
