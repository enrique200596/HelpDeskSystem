using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;

namespace HelpDeskSystem.Web.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ITicketService _ticketService; // Inyección para el evento del chat

        // Lista blanca de tipos MIME permitidos (Magic Numbers simplificados)
        private static readonly Dictionary<string, string> TiposPermitidos = new()
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".pdf", "application/pdf" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }
        };

        public ChatService(AppDbContext context, IWebHostEnvironment env, ITicketService ticketService)
        {
            _context = context;
            _env = env;
            _ticketService = ticketService;
        }

        public async Task<string> SubirArchivoAsync(IBrowserFile archivo)
        {
            // 1. Validar Extensión
            var extension = Path.GetExtension(archivo.Name).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension) || !TiposPermitidos.ContainsKey(extension))
            {
                throw new InvalidOperationException("Tipo de archivo no permitido por extensión.");
            }

            // 2. NUEVA SEGURIDAD: Validar Content-Type (MIME)
            // Esto verifica que el navegador haya detectado que el contenido coincide con la extensión.
            // Ejemplo: Si renombras virus.exe a foto.jpg, el navegador suele enviar "application/x-msdownload", no "image/jpeg".
            if (archivo.ContentType != TiposPermitidos[extension])
            {
                throw new InvalidOperationException("El archivo parece corrupto o manipulado (MIME type no coincide).");
            }

            // 3. Validar Tamaño (Máx 5MB)
            if (archivo.Size > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException("El archivo excede el límite de 5MB.");
            }

            // 4. Guardar archivo
            var carpetaDestino = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(carpetaDestino))
            {
                Directory.CreateDirectory(carpetaDestino);
            }

            // Limpieza de nombre para evitar caracteres raros
            var nombreOriginalSeguro = Regex.Replace(Path.GetFileName(archivo.Name), "[^a-zA-Z0-9._-]", "_");
            var nombreArchivo = $"{Guid.NewGuid()}_{nombreOriginalSeguro}";
            var rutaCompleta = Path.Combine(carpetaDestino, nombreArchivo);

            await using var stream = archivo.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
            await using var fs = new FileStream(rutaCompleta, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fs);

            return $"/uploads/{nombreArchivo}";
        }
        // ESTE ES EL NOMBRE CORRECTO QUE USAREMOS:
        public async Task<List<Mensaje>> ObtenerMensajesPorTicketId(int ticketId)
        {
            return await _context.Mensajes
                .Where(m => m.TicketId == ticketId)
                .Include(m => m.Usuario)
                .OrderBy(m => m.FechaHora)
                .ToListAsync();
        }
        public async Task EnviarMensaje(Mensaje mensaje)
        {
            _context.Mensajes.Add(mensaje);
            await _context.SaveChangesAsync();

            // Notificamos para que se actualice la pantalla sin recargar
            _ticketService.NotificarCambio();
        }
    }
}