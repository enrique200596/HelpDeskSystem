using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpDeskSystem.Domain.Entities
{
    /// <summary>
    /// Define qué roles de usuario tienen permisos específicos para visualizar un manual.
    /// Si un manual no tiene registros asociados en esta tabla, se considera de acceso público.
    /// </summary>
    public class ManualRolVisibilidad
    {
        /// <summary>
        /// Identificador único del registro de visibilidad.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Identificador del manual al que se le aplica la restricción de rol.
        /// </summary>
        [Required]
        public int ManualId { get; set; }

        /// <summary>
        /// Nombre del rol autorizado (ej: "Administrador", "Asesor").
        /// Se utiliza el nombre del rol para facilitar la integración con sistemas de identidad basados en claims.
        /// </summary>
        [Required(ErrorMessage = "El nombre del rol es obligatorio")]
        [MaxLength(50, ErrorMessage = "El nombre del rol no puede exceder los 50 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ]+$", ErrorMessage = "El nombre del rol solo puede contener letras")]
        public string RolNombre { get; set; } = string.Empty;

        /// <summary>
        /// Propiedad de navegación hacia el manual restringido.
        /// Declarada como virtual para habilitar Lazy Loading y mejorar la flexibilidad en consultas complejas.
        /// </summary>
        [ForeignKey(nameof(ManualId))]
        public virtual Manual? Manual { get; set; }
    }
}