using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class Sello
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // FK a ApplicationUser

        [Required]
        public string CodigoQR { get; set; } // Código QR validado

        [Required]
        public string Servicio { get; set; } // Servicio por el que se otorga el sello

        [Required]
        public DateTime FechaObtencion { get; set; }

        [Required]
        public string ReservaId { get; set; } // ID de la reserva de MS Bookings

        public bool UsadoEnPase { get; set; } // Si ya se usó para generar un pase

        // Navegación
        [ForeignKey("UserId")]
        public virtual ApplicationUser Usuario { get; set; }
    }
}