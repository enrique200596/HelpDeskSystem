using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Domain.Entities
{
    /// <summary>
    /// Representa un registro histórico de las acciones realizadas sobre un manual.
    /// Esta entidad es fundamental para el cumplimiento de normativas de auditoría y trazabilidad.
    /// </summary>
    public class ManualLog
    {
        /// <summary>
        /// Identificador único del evento de log.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Referencia al manual que fue objeto de la acción.
        /// </summary>
        [Required]
        public int ManualId { get; set; }

        /// <summary>
        /// Tipo de acción ejecutada (Creación, Edición, Eliminación, etc.).
        /// </summary>
        [Required(ErrorMessage = "La acción es obligatoria")]
        public TipoAccionManual Accion { get; set; }

        /// <summary>
        /// Descripción detallada del cambio o evento ocurrido.
        /// </summary>
        [MaxLength(500, ErrorMessage = "El detalle no puede exceder los 500 caracteres")]
        public string? Detalle { get; set; }

        /// <summary>
        /// Marca de tiempo exacta del evento en formato UTC.
        /// Se establece por defecto al momento de la creación del registro.
        /// </summary>
        [Required]
        public DateTime FechaEvento { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Identificador del usuario responsable de la acción.
        /// </summary>
        [Required]
        public Guid UsuarioId { get; set; }

        /// <summary>
        /// Propiedad de navegación hacia el usuario que realizó la acción.
        /// Declarada como virtual para soportar Lazy Loading en reportes de auditoría.
        /// </summary>
        [ForeignKey(nameof(UsuarioId))]
        public virtual Usuario? Usuario { get; set; }

        /// <summary>
        /// Propiedad de navegación hacia el manual afectado.
        /// </summary>
        [ForeignKey(nameof(ManualId))]
        public virtual Manual? Manual { get; set; }
    }
}