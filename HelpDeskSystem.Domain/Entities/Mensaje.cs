using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpDeskSystem.Domain.Entities
{
    public class Mensaje
    {
        public int Id { get; set; }

        // --- Relación con el Ticket ---
        public int TicketId { get; set; }
        public virtual Ticket? Ticket { get; set; }

        // --- ¿Quién escribió esto? ---
        public Guid UsuarioId { get; set; }
        public virtual Usuario? Usuario { get; set; }

        public string Contenido { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; } = DateTime.Now;

        // --- Para los adjuntos (Fase B) ---
        public string? AdjuntoUrl { get; set; }
    }
}
