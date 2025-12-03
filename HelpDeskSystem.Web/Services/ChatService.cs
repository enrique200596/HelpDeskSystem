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
        private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".pdf", ".xlsx" };

        public ChatService(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<string> SubirArchivoAsync(IBrowserFile archivo)
        {
            var extension = Path.GetExtension(archivo.Name);
            if (string.IsNullOrWhiteSpace(extension) || !ExtensionesPermitidas.Contains(extension))
            {
                throw new InvalidOperationException("Tipo de archivo no permitido.");
            }

            if (archivo.Size > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException("El archivo excede el límite de 5MB.");
            }

            var carpetaDestino = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(carpetaDestino))
            {
                Directory.CreateDirectory(carpetaDestino);
            }

            var nombreOriginalSeguro = Path.GetFileName(archivo.Name);
            // Eliminar caracteres problemáticos
            nombreOriginalSeguro = Regex.Replace(nombreOriginalSeguro, "[^a-zA-Z0-9._-]", "_");

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
        }
    }
}