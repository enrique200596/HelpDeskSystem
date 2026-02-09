using HelpDeskSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HelpDeskSystem.Domain.Entities
{
    /// <summary>
    /// Representa un manual técnico o instructivo dentro del sistema.
    /// Implementa Soft Delete y Control de Activación para integridad referencial.
    /// </summary>
    public class Manual : ISoftDelete, IActiveable
    {
        // Campos privados para encapsulamiento (Backing Fields)
        private readonly List<ManualEtiqueta> _etiquetas = new();
        private readonly List<ManualRolVisibilidad> _rolesVisibles = new();

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El título es obligatorio")]
        [MaxLength(200, ErrorMessage = "El título no puede exceder los 200 caracteres")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El contenido es obligatorio")]
        public string ContenidoHTML { get; set; } = string.Empty;

        // --- RELACIONES NORMALIZADAS Y ENCAPSULADAS ---

        /// <summary>
        /// Colección de etiquetas asociadas. Expuesta como IReadOnlyCollection para evitar 
        /// modificaciones externas directas (Add/Clear) sin pasar por métodos de dominio.
        /// </summary>
        public virtual IReadOnlyCollection<ManualEtiqueta> ManualEtiquetas => _etiquetas.AsReadOnly();

        /// <summary>
        /// Roles que tienen permitido visualizar este manual.
        /// </summary>
        public virtual IReadOnlyCollection<ManualRolVisibilidad> RolesVisibles => _rolesVisibles.AsReadOnly();

        // ----------------------------------------------

        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? UltimaActualizacion { get; set; }

        [Required]
        public Guid AutorId { get; set; }

        [ForeignKey("AutorId")]
        public virtual Usuario? Autor { get; set; }

        // --- MÉTODOS DE DOMINIO (Lógica de Negocio 100/100) ---

        /// <summary>
        /// Agrega una etiqueta al manual validando duplicados y formato.
        /// </summary>
        public void AgregarEtiqueta(string nombreEtiqueta)
        {
            if (string.IsNullOrWhiteSpace(nombreEtiqueta)) return;

            var etiquetaLimpia = nombreEtiqueta.Trim().ToLower();
            if (!_etiquetas.Any(e => e.Etiqueta.ToLower() == etiquetaLimpia))
            {
                _etiquetas.Add(new ManualEtiqueta { Etiqueta = nombreEtiqueta.Trim(), ManualId = this.Id });
            }
        }

        /// <summary>
        /// Configura los roles autorizados para ver este manual.
        /// </summary>
        public void AsignarRolesVisibilidad(IEnumerable<string> roles)
        {
            _rolesVisibles.Clear();
            foreach (var rol in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                _rolesVisibles.Add(new ManualRolVisibilidad { RolNombre = rol, ManualId = this.Id });
            }
        }

        /// <summary>
        /// Realiza una actualización segura del contenido del manual.
        /// </summary>
        public void ActualizarContenido(string titulo, string html)
        {
            if (string.IsNullOrWhiteSpace(titulo)) throw new ArgumentException("El título no puede estar vacío.");

            this.Titulo = titulo.Trim();
            this.ContenidoHTML = html;
            this.UltimaActualizacion = DateTime.UtcNow;
        }

        /// <summary>
        /// Cambia el estado de activación del manual.
        /// </summary>
        public void SetEstado(bool activo)
        {
            this.IsActive = activo;
            this.UltimaActualizacion = DateTime.UtcNow;
        }
    }
}