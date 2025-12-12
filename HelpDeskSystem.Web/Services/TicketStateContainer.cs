// HelpDeskSystem.Web/Services/TicketStateContainer.cs
namespace HelpDeskSystem.Web.Services
{
    public class TicketStateContainer
    {
        // Firma actualizada: ID, Título Ticket, Nombre Remitente
        public event Action<int?, string?, string?>? OnChange;

        public void NotifyStateChanged(int? ticketId = null, string? titulo = null, string? remitente = null)
        {
            OnChange?.Invoke(ticketId, titulo, remitente);
        }
    }
}