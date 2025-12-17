using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpDeskSystem.Domain.Entities
{
    public class Manual
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El título es obligatorio")]
        [MaxLength(200)]
        public string Titulo { get; set; } = string.Empty;

        // Aquí guardaremos el HTML generado por el editor de texto
        [Required(ErrorMessage = "El contenido es obligatorio")]
        public string ContenidoHTML { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? UltimaActualizacion { get; set; }

        // Relación con el usuario que creó/editó el manual
        public Guid AutorId { get; set; }

        [ForeignKey("AutorId")]
        public Usuario? Autor { get; set; }
    }
}