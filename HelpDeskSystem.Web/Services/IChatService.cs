using HelpDeskSystem.Domain.Entities;
using Microsoft.AspNetCore.Components.Forms;

namespace HelpDeskSystem.Web.Services
{
    public interface IChatService
    {
        Task<List<Mensaje>> ObtenerMensajesPorTicketId(int ticketId);
        Task EnviarMensaje(Mensaje mensaje);
        Task<string> SubirArchivoAsync(IBrowserFile archivo);
    }
}