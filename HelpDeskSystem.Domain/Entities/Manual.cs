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

        [Required(ErrorMessage = "El contenido es obligatorio")]
        public string ContenidoHTML { get; set; } = string.Empty;

        // --- NUEVOS CAMPOS ---

        // Guardaremos las etiquetas separadas por comas. Ej: "Redes,Wifi,Configuración"
        [MaxLength(500)]
        public string Etiquetas { get; set; } = string.Empty;

        // Guardaremos los roles permitidos separados por comas. Ej: "Administrador,Asesor"
        // Si está vacío, asumiremos que es público para todos.
        [MaxLength(200)]
        public string RolesVisibles { get; set; } = string.Empty;

        // Control de eliminación lógica (Soft Delete)
        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true; // Para borrador/publicado

        // ---------------------

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? UltimaActualizacion { get; set; }

        public Guid AutorId { get; set; }

        [ForeignKey("AutorId")]
        public Usuario? Autor { get; set; }
    }
}