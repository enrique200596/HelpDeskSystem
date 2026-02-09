using HelpDeskSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpDeskSystem.Domain.Entities
{
    public class Manual : ISoftDelete, IActiveable
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El título es obligatorio")]
        [MaxLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El contenido es obligatorio")]
        public string ContenidoHTML { get; set; } = string.Empty;

        // --- CORRECCIÓN: NORMALIZACIÓN DE DATOS (100/100) ---
        // Reemplazamos los strings CSV por tablas relacionales para integridad y consultas eficientes.

        // Relación con Etiquetas (Antes string separada por comas)
        public virtual ICollection<ManualEtiqueta> ManualEtiquetas { get; set; } = new List<ManualEtiqueta>();

        // Relación con Roles de Visibilidad (Antes string separada por comas)
        public virtual ICollection<ManualRolVisibilidad> RolesVisibles { get; set; } = new List<ManualRolVisibilidad>();

        // ---------------------

        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? UltimaActualizacion { get; set; }

        public Guid AutorId { get; set; }

        [ForeignKey("AutorId")]
        public Usuario? Autor { get; set; }
    }
}