using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Web.Services
{
    public class TicketStateContainer
    {
        // Nueva firma: ID Ticket, Título, Tipo Evento, Nombre Ejecutor, ID Dueño, ID Asesor
        public event Action<int?, string?, TipoNotificacion, string?, Guid?, Guid?>? OnChange;

        public void NotifyStateChanged(int? ticketId, string? titulo, TipoNotificacion tipo, string? nombreEjecutor, Guid? ownerId, Guid? asesorId)
        {
            OnChange?.Invoke(ticketId, titulo, tipo, nombreEjecutor, ownerId, asesorId);
        }
    }
}