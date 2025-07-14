using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class CodigoQR
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Codigo { get; set; } // El código QR único

        [Required]
        public string UserId { get; set; } // FK a ApplicationUser

        [Required]
        public string ReservaId { get; set; } // ID de la reserva de MS Bookings

        [Required]
        public string Servicio { get; set; }

        [Required]
        public DateTime FechaGeneracion { get; set; }

        [Required]
        public DateTime FechaExpiracion { get; set; }

        public bool Validado { get; set; } // Si ya fue validado

        public DateTime? FechaValidacion { get; set; }

        public string ValidadoPor { get; set; } // Usuario que validó

        // Navegación
        [ForeignKey("UserId")]
        public virtual ApplicationUser Usuario { get; set; }
    }
}