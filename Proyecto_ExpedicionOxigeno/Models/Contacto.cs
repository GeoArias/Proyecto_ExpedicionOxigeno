using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_ExpedicionOxigeno.Models
{
    [Table("Contactos")]
    public class Contacto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "La consulta es obligatoria.")]
        [StringLength(1000, ErrorMessage = "La consulta no puede superar los 1000 caracteres.")]
        public string Consulta { get; set; }

        public string Nombre { get; set; }

        [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
        [StringLength(20, ErrorMessage = "El número de teléfono no puede superar los 20 caracteres.")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Debe ingresar un correo válido.")]
        [StringLength(100, ErrorMessage = "El correo electrónico no puede superar los 100 caracteres.")]
        public string Email { get; set; }

        public DateTime Fecha { get; set; }

        public bool Respondida { get; set; } = false;

        public Contacto()
        {
            Fecha = DateTime.Now;
        }
    }
}
