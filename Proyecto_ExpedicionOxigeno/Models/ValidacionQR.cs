using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class ValidacionQR
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string CodigoQR { get; set; }

        [Required]
        public DateTime FechaIntento { get; set; }

        [Required]
        public bool Exitoso { get; set; }

        [Required]
        public string DireccionIP { get; set; }

        public string MotivoFallo { get; set; } // Razón del fallo si no fue exitoso

        public string ValidadoPor { get; set; } // Usuario que intentó validar
    }

}