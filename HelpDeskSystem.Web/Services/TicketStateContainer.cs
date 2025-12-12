// Ubicación: HelpDeskSystem.Web/Services/TicketStateContainer.cs
namespace HelpDeskSystem.Web.Services
{
    public class TicketStateContainer
    {
        // Este evento será escuchado por TODOS los usuarios conectados
        public event Action? OnChange;

        public void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}