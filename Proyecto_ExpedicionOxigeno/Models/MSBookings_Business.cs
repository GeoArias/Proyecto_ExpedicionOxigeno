        using System;
        using System.Collections.Generic;
        using System.ComponentModel.DataAnnotations;
        using System.ComponentModel.DataAnnotations.Schema;

        namespace Proyecto_ExpedicionOxigeno.Models
        {
            public class MSBookings_Business
            {
                [Key]
                [Column("id")]
                [Required]
                public Guid Id { get; set; }

                [Column("displayName")]
                [Required]
                [MaxLength(256)]
                public string DisplayName { get; set; }

                [Column("createdDateTime")]
                public DateTime? CreatedDateTime { get; set; }

                [Column("lastUpdatedDateTime")]
                public DateTime? LastUpdatedDateTime { get; set; }

                [Column("businessType")]
                [MaxLength(128)]
                public string BusinessType { get; set; }

                [Column("phone")]
                [MaxLength(64)]
                public string Phone { get; set; }

                [Column("email")]
                [MaxLength(256)]
                public string Email { get; set; }

                [Column("webSiteUrl")]
                [MaxLength(512)]
                public string WebSiteUrl { get; set; }

                [Column("defaultCurrencyIso")]
                [MaxLength(8)]
                public string DefaultCurrencyIso { get; set; }

                [Column("isPublished")]
                public bool? IsPublished { get; set; }

                [Column("publicUrl")]
                [MaxLength(512)]
                public string PublicUrl { get; set; }

                [Column("languageTag")]
                [MaxLength(16)]
                public string LanguageTag { get; set; }

                [Column("address")]
                public MSBookings_Address Address { get; set; }

                [Column("businessHours")]
                public List<MSBookings_BusinessHour> BusinessHours { get; set; }

                [Column("schedulingPolicy")]
                public MSBookings_SchedulingPolicy SchedulingPolicy { get; set; }

                [Column("bookingPageSettings")]
                public MSBookings_BookingPageSettings BookingPageSettings { get; set; }
            }
    

            public class MSBookings_BusinessHour
            {
                [Column("day")]
                public string Day { get; set; }

                [Column("timeSlots")]
                public List<MSBookings_TimeSlot> TimeSlots { get; set; }
            }

            public class MSBookings_TimeSlot
            {
                [Column("startTime")]
                public string StartTime { get; set; }

                [Column("endTime")]
                public string EndTime { get; set; }
            }

            public class MSBookings_GeneralAvailability
            {
                [Column("availabilityType")]
                public string AvailabilityType { get; set; }

                [Column("businessHours")]
                public List<MSBookings_BusinessHour> BusinessHours { get; set; }
            }

            public class MSBookings_BookingPageSettings
            {
                [Column("privacyPolicyWebUrl")]
                public string PrivacyPolicyWebUrl { get; set; }

                [Column("termsAndConditionsWebUrl")]
                public string TermsAndConditionsWebUrl { get; set; }

                [Column("enforceOneTimePassword")]
                public bool? EnforceOneTimePassword { get; set; }

                [Column("accessControl")]
                public string AccessControl { get; set; }

                [Column("isCustomerConsentEnabled")]
                public bool? IsCustomerConsentEnabled { get; set; }

                [Column("customerConsentMessage")]
                public string CustomerConsentMessage { get; set; }

                [Column("isBusinessLogoDisplayEnabled")]
                public bool? IsBusinessLogoDisplayEnabled { get; set; }

                [Column("isSearchEngineIndexabilityDisabled")]
                public bool? IsSearchEngineIndexabilityDisabled { get; set; }

                [Column("bookingPageColorCode")]
                public string BookingPageColorCode { get; set; }

                [Column("businessTimeZone")]
                public string BusinessTimeZone { get; set; }

                [Column("isTimeSlotTimeZoneSetToBusinessTimeZone")]
                public bool? IsTimeSlotTimeZoneSetToBusinessTimeZone { get; set; }
            }
        }