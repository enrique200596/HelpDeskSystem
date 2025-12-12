// HelpDeskSystem.Web/Services/TicketStateContainer.cs
namespace HelpDeskSystem.Web.Services
{
    public class TicketStateContainer
    {
        // Firma: ID, Título, Remitente, ID Dueño Ticket, ID Asesor Ticket
        public event Action<int?, string?, string?, Guid?, Guid?>? OnChange;

        public void NotifyStateChanged(int? ticketId = null, string? titulo = null, string? remitente = null, Guid? ownerId = null, Guid? asesorId = null)
        {
            OnChange?.Invoke(ticketId, titulo, remitente, ownerId, asesorId);
        }
    }
}