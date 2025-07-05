using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;

// The main response model
public class BookingStaffAvailabilityCollectionResponse
{
    public List<BookingStaffAvailability> Value { get; set; }
}

// Individual staff availability
public class BookingStaffAvailability
{
    public string StaffId { get; set; }
    public List<BookingStaffAvailabilityItem> AvailabilityItems { get; set; }
}

// Specific time slots
public class BookingStaffAvailabilityItem
{
    public DateTimeOffset StartDateTime { get; set; }
    public DateTimeOffset EndDateTime { get; set; }
    public string Status { get; set; }
}