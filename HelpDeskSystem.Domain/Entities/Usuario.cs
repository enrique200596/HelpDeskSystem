using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Domain.Entities
{
    public class Usuario
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FotoPerfilUrl { get; set; } = string.Empty; // Para tu requisito de foto
        public RolUsuario Rol { get; set; } // Admin, Asesor, Usuario
        public bool IsActive { get; set; } = true; // Para poder desactivarlos sin borrar
        // En un sistema real, aquí guardaríamos el HASH, no el texto plano.
        // Por simplicidad educativa hoy, lo usaremos directo, pero tenlo en mente.
        public string Password { get; set; } = string.Empty;
        // Relación: Qué categorías puede atender este asesor
        public virtual ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
    }
}
