using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class MSBookings_Businesses
    {
        [Key]
        [Column("id")]
        [Required]
        public string Id { get; set; }

        [Column("displayName")]
        [Required]
        [MaxLength(256)]
        public string DisplayName { get; set; }
    }
}