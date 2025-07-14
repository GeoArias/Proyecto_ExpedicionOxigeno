using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class PaseExpedicion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // FK a ApplicationUser

        [Required]
        public string CodigoPase { get; set; } // Código único del pase

        [Required]
        public DateTime FechaGeneracion { get; set; }

        [Required]
        public DateTime FechaExpiracion { get; set; }

        public bool Utilizado { get; set; } // Si ya fue usado

        public DateTime? FechaUso { get; set; }

        public string SellosUsados { get; set; } // IDs de los sellos usados (JSON)

        // Navegación
        [ForeignKey("UserId")]
        public virtual ApplicationUser Usuario { get; set; }
    }
}
