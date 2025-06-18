using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class PuntosFidelidad
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPunto { get; set; }

        [Required]
        [ForeignKey("Usuario")]
        public string IdUsuario { get; set; } // Relación con el Usuario de .NET Identity    

        public virtual ApplicationUser Usuario { get; set; } // Navegación hacia la entidad Usuario    

        [Required]
        public DateTime FechaAsignacion { get; set; }

        [Required]
        public int CantidadPuntos { get; set; }

        [Required]
        [MaxLength(255)]
        public string Motivo { get; set; }

        [Required]
        [ForeignKey("Estado")]
        public int IdEstado { get; set; } // Relación con la entidad Estado  

        public virtual Estado Estado { get; set; } // Navegación hacia la entidad Estado  
    }
}