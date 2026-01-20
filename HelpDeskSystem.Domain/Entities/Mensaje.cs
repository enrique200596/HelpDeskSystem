using System;

namespace HelpDeskSystem.Domain.Entities
{
    public class Mensaje
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public virtual Ticket? Ticket { get; set; }

        public Guid UsuarioId { get; set; }
        public virtual Usuario? Usuario { get; set; }

        public string Contenido { get; set; } = string.Empty;

        // Cambio a UtcNow para consistencia global
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;

        public string? AdjuntoUrl { get; set; }
    }
}