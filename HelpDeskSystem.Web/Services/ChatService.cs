using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace HelpDeskSystem.Web.Services
{
    public class ChatService : IChatService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IWebHostEnvironment _env;
        private readonly TicketStateContainer _stateContainer; // Usamos el contenedor directamente
        private readonly ILogger<ChatService> _logger;

        private static readonly Dictionary<string, string> TiposPermitidos = new()
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".pdf", "application/pdf" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }
        };

        public ChatService(
            IDbContextFactory<AppDbContext> dbFactory,
            IWebHostEnvironment env,
            TicketStateContainer stateContainer,
            ILogger<ChatService> logger)
        {
            _dbFactory = dbFactory;
            _env = env;
            _stateContainer = stateContainer;
            _logger = logger;
        }

        public async Task<string> SubirArchivoAsync(IBrowserFile archivo)
        {
            try
            {
                var extension = Path.GetExtension(archivo.Name).ToLowerInvariant();
                if (!TiposPermitidos.ContainsKey(extension))
                    throw new InvalidOperationException("Tipo de archivo no permitido.");

                if (archivo.Size > 5 * 1024 * 1024)
                    throw new InvalidOperationException("El archivo excede los 5MB.");

                var carpetaDestino = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);

                // Sanitización y nombre único (Capa de Seguridad 5)
                var nombreArchivo = $"{Guid.NewGuid()}{extension}";
                var rutaCompleta = Path.Combine(carpetaDestino, nombreArchivo);

                await using var stream = archivo.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
                await using var fs = new FileStream(rutaCompleta, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(fs);

                return $"/uploads/{nombreArchivo}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir archivo de chat: {Name}", archivo.Name);
                throw;
            }
        }

        public async Task<List<Mensaje>> ObtenerMensajesPorTicketId(int ticketId)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Mensajes
                .Where(m => m.TicketId == ticketId)
                .Include(m => m.Usuario)
                .OrderBy(m => m.FechaHora)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task EnviarMensaje(Mensaje mensaje)
        {
            using var context = _dbFactory.CreateDbContext();

            var ticket = await context.Tickets.FindAsync(mensaje.TicketId);
            if (ticket == null) throw new InvalidOperationException("Ticket no encontrado.");

            // Reglas de Negocio
            if (ticket.Estado == EstadoTicket.Resuelto)
                throw new InvalidOperationException("No se pueden enviar mensajes a un ticket cerrado.");

            if (ticket.AsesorId == null)
                throw new InvalidOperationException("El ticket aún no tiene un asesor asignado.");

            mensaje.FechaHora = DateTime.UtcNow;
            context.Mensajes.Add(mensaje);
            await context.SaveChangesAsync();

            // Obtener datos del remitente para la notificación
            var usuario = await context.Usuarios.FindAsync(mensaje.UsuarioId);

            // Notificamos usando el nuevo sistema de Enums
            _stateContainer.NotifyStateChanged(
                mensaje.TicketId,
                ticket.Titulo,
                TipoNotificacion.NuevoMensajeChat,
                usuario?.Nombre ?? "Usuario",
                ticket.UsuarioId,
                ticket.AsesorId);
        }
    }
}