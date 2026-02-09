using HelpDeskSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HelpDeskSystem.Domain.Entities
{
    public class Manual : ISoftDelete, IActiveable
    {
        private readonly List<ManualEtiqueta> _etiquetas = new();
        private readonly List<ManualRolVisibilidad> _rolesVisibles = new();

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El título es obligatorio")]
        [MaxLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El contenido es obligatorio")]
        public string ContenidoHTML { get; set; } = string.Empty;

        public virtual IReadOnlyCollection<ManualEtiqueta> ManualEtiquetas => _etiquetas.AsReadOnly();
        public virtual IReadOnlyCollection<ManualRolVisibilidad> RolesVisibles => _rolesVisibles.AsReadOnly();

        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? UltimaActualizacion { get; set; }

        [Required]
        public Guid AutorId { get; set; }

        [ForeignKey("AutorId")]
        public virtual Usuario? Autor { get; set; }

        public void ActualizarContenido(string titulo, string html)
        {
            this.Titulo = titulo?.Trim() ?? string.Empty;
            this.ContenidoHTML = html;
            this.UltimaActualizacion = DateTime.UtcNow;
        }

        public void AsignarEtiquetas(IEnumerable<string> etiquetas)
        {
            _etiquetas.Clear();
            foreach (var et in etiquetas.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                _etiquetas.Add(new ManualEtiqueta { Etiqueta = et.Trim(), ManualId = this.Id });
            }
        }

        public void AsignarRolesVisibilidad(IEnumerable<string> roles)
        {
            _rolesVisibles.Clear();
            foreach (var rol in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                _rolesVisibles.Add(new ManualRolVisibilidad { RolNombre = rol, ManualId = this.Id });
            }
        }

        public void SetEstado(bool activo)
        {
            this.IsActive = activo;
            this.UltimaActualizacion = DateTime.UtcNow;
        }
    }
}