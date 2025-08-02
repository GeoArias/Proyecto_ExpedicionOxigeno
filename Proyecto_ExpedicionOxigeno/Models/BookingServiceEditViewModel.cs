using System;
using System.Collections.Generic;

public class BookingServiceEditViewModel
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Notes { get; set; }
    public decimal DefaultPrice { get; set; }
    public int DurationHours { get; set; }
    public int DurationMinutes { get; set; }
    public int PreBufferHours { get; set; }
    public int PreBufferMinutes { get; set; }
    public int PostBufferHours { get; set; }
    public int PostBufferMinutes { get; set; }
    public List<string> SelectedStaff { get; set; }
    public List<CustomAvailabilityViewModel> CustomAvailabilities { get; set; }
}

public class CustomAvailabilityViewModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string AvailabilityType { get; set; }
    public List<BusinessHourViewModel> BusinessHours { get; set; }
}

public class BusinessHourViewModel
{
    public string Day { get; set; }
    public List<TimeSlotViewModel> TimeSlots { get; set; }
}

public class TimeSlotViewModel
{
    public string StartTime { get; set; } // "HH:mm"
    public string EndTime { get; set; }   // "HH:mm"
}