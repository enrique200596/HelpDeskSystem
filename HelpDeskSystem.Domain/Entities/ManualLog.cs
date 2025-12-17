using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpDeskSystem.Domain.Entities
{
    public class ManualLog
    {
        [Key]
        public int Id { get; set; }

        public int ManualId { get; set; }

        // Acción realizada: "Creación", "Edición", "Desactivación", "Eliminación Definitiva"
        [Required]
        [MaxLength(50)]
        public string Accion { get; set; } = string.Empty;

        // Detalles opcionales (ej: "Cambió el título")
        public string? Detalle { get; set; }

        public DateTime FechaEvento { get; set; } = DateTime.Now;

        // Quién realizó la acción
        public Guid UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        // Relación opcional con el manual (por si se borra físicamente, podríamos dejar el log huérfano o borrarlo en cascada)
        [ForeignKey("ManualId")]
        public Manual? Manual { get; set; }
    }
}