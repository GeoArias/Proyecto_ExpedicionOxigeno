﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public class Review
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Nombre { get; set; }

        [Required(ErrorMessage = "El comentario es obligatorio.")]
        public string Comentario { get; set; }

        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5.")]
        public int Calificacion { get; set; }

        public DateTime Fecha { get; set; }

        public bool Mostrar { get; set; }

        public string Servicio { get; set; } // ✅ NUEVO CAMPO

        public Review()
        {
            Fecha = DateTime.Now;
        }
    }
}
