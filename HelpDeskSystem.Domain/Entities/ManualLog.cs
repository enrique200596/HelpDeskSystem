using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Domain.Entities
{
    public class ManualLog
    {
        [Key]
        public int Id { get; set; }

        public int ManualId { get; set; }

        [Required]
        public TipoAccionManual Accion { get; set; } // Cambio a Enum

        public string? Detalle { get; set; }

        public DateTime FechaEvento { get; set; } = DateTime.UtcNow; // Cambio a UTC

        public Guid UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario? Usuario { get; set; }

        [ForeignKey("ManualId")]
        public virtual Manual? Manual { get; set; }
    }
}