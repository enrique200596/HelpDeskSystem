using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Web.Services
{
    /// <summary>
    /// Contenedor de estado Singleton para la gestión de eventos en tiempo real.
    /// CORRECCIÓN: Implementación robusta para evitar que fallos en un suscriptor
    /// afecten a toda la cadena de notificaciones del sistema.
    /// </summary>
    public class TicketStateContainer
    {
        // Firma del evento: TicketId, Título, Tipo, NombreEjecutor, OwnerId, AsesorId
        public event Action<int?, string?, TipoNotificacion, string?, Guid?, Guid?>? OnChange;

        /// <summary>
        /// Notifica un cambio de estado a todos los componentes escuchando (circuitos activos).
        /// </summary>
        public void NotifyStateChanged(int? ticketId, string? titulo, TipoNotificacion tipo, string? nombreEjecutor, Guid? ownerId, Guid? asesorId)
        {
            // Capturamos la lista actual de suscriptores para evitar condiciones de carrera
            var handler = OnChange;
            if (handler == null) return;

            // Obtenemos cada delegado (cada pestaña/circuito de Blazor abierto)
            var delegates = handler.GetInvocationList();

            foreach (var del in delegates)
            {
                try
                {
                    // CORRECCIÓN: Invocación aislada. 
                    // Cast al tipo específico de la acción para mantener el rendimiento.
                    var action = (Action<int?, string?, TipoNotificacion, string?, Guid?, Guid?>)del;

                    // Ejecutamos cada notificación de forma independiente. Si un usuario
                    // tiene el circuito inestable, el error se captura aquí y no detiene
                    // el envío de la notificación al resto de los compañeros.
                    action.Invoke(ticketId, titulo, tipo, nombreEjecutor, ownerId, asesorId);
                }
                catch
                {
                    // Silenciamos excepciones de circuitos individuales (JS disconnected, etc.)
                    // para garantizar que el motor de notificaciones sea ininterrumpible.
                }
            }
        }
    }
}