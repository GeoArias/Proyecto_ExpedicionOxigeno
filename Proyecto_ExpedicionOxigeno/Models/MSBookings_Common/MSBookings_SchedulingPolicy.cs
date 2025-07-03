using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class MSBookings_SchedulingPolicy
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Service")]
        public string ServiceId { get; set; }

        [Column("timeSlotInterval")]
        public string TimeSlotInterval { get; set; }
        
        public TimeSpan TimeSlotIntervalTimeSpan { get; set; }

        [Column("minimumLeadTime")]
        public string MinimumLeadTime { get; set; }
        
        public TimeSpan MinimumLeadTimeTimeSpan { get; set; }

        [Column("maximumAdvance")]
        public string MaximumAdvance { get; set; }
        
        public TimeSpan MaximumAdvanceTimeSpan { get; set; }

        [Column("sendConfirmationsToOwner")]
        public bool? SendConfirmationsToOwner { get; set; }

        [Column("allowStaffSelection")]
        public bool? AllowStaffSelection { get; set; }

        [Column("isMeetingInviteToCustomersEnabled")]
        public bool? IsMeetingInviteToCustomersEnabled { get; set; }

        [Column("generalAvailability")]
        public MSBookings_GeneralAvailability GeneralAvailability { get; set; }

        [Column("customAvailabilities")]
        public List<object> CustomAvailabilities { get; set; }

        public virtual MSBookings_Service Service { get; set; }
    }
}   