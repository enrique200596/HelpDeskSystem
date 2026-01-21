using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging; // Capa de auditoría y trazabilidad
using System.Security.Claims;

namespace HelpDeskSystem.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // CAPA 1: Solo usuarios autenticados
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UploadController> _logger; // Inyección para auditoría técnica

        // Constantes de validación centralizadas
        private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".gif" };
        private const long MaximoTamañoBytes = 5 * 1024 * 1024; // 5 MB

        public UploadController(IWebHostEnvironment environment, ILogger<UploadController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        [HttpPost("imagen")]
        public async Task<IActionResult> SubirImagen(IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.Sid);
            var userName = User.Identity?.Name;

            // CAPA 2: Validación de existencia y nulidad
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Intento de subida vacío por el usuario {User} ({Id})", userName, userId);
                return BadRequest("No se proporcionó un archivo válido.");
            }

            // CAPA 3: Validación de tamaño estricta
            if (file.Length > MaximoTamañoBytes)
            {
                _logger.LogWarning("Archivo excede el límite ({} bytes) enviado por {User}", file.Length, userName);
                return BadRequest("El archivo excede el límite de 5MB.");
            }

            // CAPA 4: Validación de extensión (Whitelist)
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
            {
                _logger.LogSecurity("Intento de subir extensión no permitida: {Ext} por usuario {User}", extension, userName);
                return BadRequest("Formato de imagen no permitido.");
            }

            try
            {
                var pathCarpeta = Path.Combine(_environment.WebRootPath, "uploads", "manuales");
                if (!Directory.Exists(pathCarpeta))
                {
                    Directory.CreateDirectory(pathCarpeta);
                    _logger.LogInformation("Carpeta de almacenamiento creada en: {Path}", pathCarpeta);
                }

                // CAPA 5: Sanitización absoluta del nombre (Prevención de Path Traversal)
                // Se ignora el nombre original y se genera uno nuevo con Guid
                var nombreArchivo = $"{Guid.NewGuid()}{extension}";
                var pathCompleto = Path.Combine(pathCarpeta, nombreArchivo);

                _logger.LogInformation("Iniciando escritura de archivo: {Nombre} para usuario {User}", nombreArchivo, userName);

                using (var stream = new FileStream(pathCompleto, FileMode.Create, FileAccess.Write))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("Imagen subida con éxito. URL: /uploads/manuales/{Nombre}", nombreArchivo);

                var url = $"/uploads/manuales/{nombreArchivo}";
                return Ok(new { url });
            }
            catch (Exception ex)
            {
                // AUDITORÍA 10/10: Registramos el error real en los logs del servidor
                _logger.LogError(ex, "Error crítico procesando subida de archivo para {User}", userName);

                // Respuesta segura: No revelamos rutas ni errores de sistema al cliente
                return StatusCode(500, "Error interno al procesar la imagen en el servidor.");
            }
        }
    }
}