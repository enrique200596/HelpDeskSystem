using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpDeskSystem.Domain.Entities
{
    public class Categoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        // Relación Muchos a Muchos con Usuarios (Asesores)
        public virtual ICollection<Usuario> Asesores { get; set; } = new List<Usuario>();
    }
}