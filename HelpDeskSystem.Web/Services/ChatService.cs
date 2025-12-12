using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums; // Asegúrate de tener este using
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;

namespace HelpDeskSystem.Web.Services
{
    public class ChatService : IChatService
    {
        // Usamos Factory aquí también
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IWebHostEnvironment _env;
        private readonly ITicketService _ticketService;

        private static readonly Dictionary<string, string> TiposPermitidos = new()
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".pdf", "application/pdf" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }
        };

        public ChatService(IDbContextFactory<AppDbContext> dbFactory, IWebHostEnvironment env, ITicketService ticketService)
        {
            _dbFactory = dbFactory; // Inyección de fábrica
            _env = env;
            _ticketService = ticketService;
        }

        public async Task<string> SubirArchivoAsync(IBrowserFile archivo)
        {
            // ... (Tu lógica de validación de archivos queda IDÉNTICA) ...
            // Solo copia la parte interna del método que ya tenías, no cambia nada de lógica.

            // 1. Validar Extensión
            var extension = Path.GetExtension(archivo.Name).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension) || !TiposPermitidos.ContainsKey(extension))
                throw new InvalidOperationException("Tipo de archivo no permitido.");

            if (archivo.ContentType != TiposPermitidos[extension])
                throw new InvalidOperationException("Archivo corrupto o manipulado.");

            if (archivo.Size > 5 * 1024 * 1024) throw new InvalidOperationException("Máx 5MB.");

            var carpetaDestino = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);

            var nombreOriginalSeguro = Regex.Replace(Path.GetFileName(archivo.Name), "[^a-zA-Z0-9._-]", "_");
            var nombreArchivo = $"{Guid.NewGuid()}_{nombreOriginalSeguro}";
            var rutaCompleta = Path.Combine(carpetaDestino, nombreArchivo);

            await using var stream = archivo.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
            await using var fs = new FileStream(rutaCompleta, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fs);

            return $"/uploads/{nombreArchivo}";
        }

        public async Task<List<Mensaje>> ObtenerMensajesPorTicketId(int ticketId)
        {
            using var context = _dbFactory.CreateDbContext(); // Contexto limpio
            return await context.Mensajes
                .Where(m => m.TicketId == ticketId)
                .Include(m => m.Usuario)
                .OrderBy(m => m.FechaHora)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task EnviarMensaje(Mensaje mensaje)
        {
            using var context = _dbFactory.CreateDbContext(); // Contexto limpio

            // 1. BLINDAJE: Verificar si el ticket está cerrado
            var ticket = await context.Tickets.FindAsync(mensaje.TicketId);
            if (ticket == null) throw new Exception("Ticket no encontrado");

            if (ticket.Estado == EstadoTicket.Resuelto)
            {
                throw new InvalidOperationException("El ticket está cerrado. No se pueden enviar mensajes.");
            }

            // 2. Guardar Mensaje
            context.Mensajes.Add(mensaje);
            await context.SaveChangesAsync();

            // 3. Preparar Notificación
            string nombreRemitente = "Usuario";
            if (mensaje.Usuario != null) nombreRemitente = mensaje.Usuario.Nombre;
            else
            {
                var usuario = await context.Usuarios.FindAsync(mensaje.UsuarioId);
                if (usuario != null) nombreRemitente = usuario.Nombre;
            }

            // Notificar a todos (Dashboard, Detalle, etc.)
            _ticketService.NotificarCambio(mensaje.TicketId, ticket.Titulo, nombreRemitente);
        }
    }
}