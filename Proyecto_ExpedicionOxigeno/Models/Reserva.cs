using System;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class Reserva
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(256)]
        public string Usuario { get; set; } // Correo

        [Required]
        [StringLength(256)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(256)]
        public string TipoActividad { get; set; }

        [Required]
        public DateTime DiaReserva { get; set; }

        [Required]
        public DateTime HoraInicio { get; set; } // Hora de inicio de la reserva

        [Required]
        public DateTime HoraFin { get; set; }    // Hora de fin de la reserva
    }
}