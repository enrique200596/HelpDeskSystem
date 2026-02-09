using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpDeskSystem.Domain.Entities
{
    /// <summary>
    /// Representa una etiqueta o palabra clave asociada a un manual para facilitar su categorización y búsqueda.
    /// </summary>
    public class ManualEtiqueta
    {
        /// <summary>
        /// Identificador único de la relación etiqueta-manual.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Identificador del manual al que pertenece la etiqueta.
        /// </summary>
        [Required]
        public int ManualId { get; set; }

        /// <summary>
        /// Texto de la etiqueta.
        /// </summary>
        [Required(ErrorMessage = "El texto de la etiqueta es obligatorio")]
        [MaxLength(50, ErrorMessage = "La etiqueta no puede superar los 50 caracteres")]
        [RegularExpression(@"^[a-zA-Z0-9\sñÑáéíóúÁÉÍÓÚ]+$", ErrorMessage = "La etiqueta contiene caracteres no permitidos")]
        public string Etiqueta { get; set; } = string.Empty;

        /// <summary>
        /// Propiedad de navegación hacia el manual padre.
        /// Se marca como virtual para permitir la carga diferida (Lazy Loading).
        /// </summary>
        [ForeignKey(nameof(ManualId))]
        public virtual Manual? Manual { get; set; }
    }
}