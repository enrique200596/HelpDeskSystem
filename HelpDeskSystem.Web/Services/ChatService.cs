using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace HelpDeskSystem.Web.Services
{
    public class ChatService : IChatService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IWebHostEnvironment _env;
        private readonly TicketStateContainer _stateContainer;
        private readonly ILogger<ChatService> _logger;

        // Extensiones permitidas por seguridad institucional
        private static readonly Dictionary<string, string> TiposPermitidos = new()
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".pdf", "application/pdf" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
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
                    throw new InvalidOperationException("Formato de archivo no permitido por políticas de seguridad.");

                // Límite de 5MB por archivo para evitar saturación del servidor
                const long maxFileSize = 5 * 1024 * 1024;
                if (archivo.Size > maxFileSize)
                    throw new InvalidOperationException("El archivo excede el límite permitido de 5MB.");

                var carpetaDestino = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(carpetaDestino))
                    Directory.CreateDirectory(carpetaDestino);

                // Nombre único para evitar colisiones y ataques de sobreescritura
                var nombreArchivo = $"{Guid.NewGuid()}{extension}";
                var rutaCompleta = Path.Combine(carpetaDestino, nombreArchivo);

                // Apertura de flujo segura con límite de tamaño explícito
                await using var stream = archivo.OpenReadStream(maxAllowedSize: maxFileSize);
                await using var fs = new FileStream(rutaCompleta, FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(fs);

                return $"/uploads/{nombreArchivo}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al procesar archivo adjunto: {Name}", archivo.Name);
                throw;
            }
        }

        public async Task<List<Mensaje>> ObtenerMensajesPorTicketId(int ticketId)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Mensajes
                .AsNoTracking() // Optimización: Los mensajes de chat son de solo lectura en el listado
                .Include(m => m.Usuario)
                .Where(m => m.TicketId == ticketId)
                .OrderBy(m => m.FechaHora)
                .ToListAsync();
        }

        public async Task EnviarMensaje(Mensaje mensaje)
        {
            using var context = _dbFactory.CreateDbContext();

            // Carga del ticket sin filtros globales para permitir chat en tickets resueltos si fuera necesario (opcional)
            var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.Id == mensaje.TicketId);

            if (ticket == null)
                throw new InvalidOperationException("El ticket de destino no existe.");

            // CORRECCIÓN DE LÓGICA: Se permite enviar mensajes si el ticket está abierto o asignado.
            if (ticket.Estado == EstadoTicket.Resuelto)
                throw new InvalidOperationException("El ticket ya ha sido resuelto. No se admiten nuevos mensajes.");

            mensaje.FechaHora = DateTime.UtcNow;
            context.Mensajes.Add(mensaje);
            await context.SaveChangesAsync();

            // CORRECCIÓN CS1061: Se cambia el orden para filtrar por Id antes de proyectar solo el Nombre.
            var usuario = await context.Usuarios
                .AsNoTracking()
                .Where(u => u.Id == mensaje.UsuarioId)
                .Select(u => new { u.Nombre })
                .FirstOrDefaultAsync();

            // Sincronización con el contenedor de estado para la reactividad de Blazor
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