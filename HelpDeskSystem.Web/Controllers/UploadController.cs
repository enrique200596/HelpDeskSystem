using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // Necesario para [Authorize]

namespace HelpDeskSystem.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // CAPA 1: Solo usuarios logueados pueden subir archivos
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        // Definimos constantes de validación
        private readonly string[] _extensionesPermitidas = { ".jpg", ".jpeg", ".png", ".gif" };
        private const long _maximoTamañoBytes = 5 * 1024 * 1024; // 5 MB

        public UploadController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost("imagen")]
        public async Task<IActionResult> SubirImagen(IFormFile file)
        {
            // CAPA 2: Validación básica de existencia
            if (file == null || file.Length == 0)
                return BadRequest("No se proporcionó un archivo válido.");

            // CAPA 3: Validación de tamaño
            if (file.Length > _maximoTamañoBytes)
                return BadRequest("El archivo excede el límite de 5MB.");

            // CAPA 4: Validación de extensión (Whitelist)
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_extensionesPermitidas.Contains(extension))
                return BadRequest("Formato de imagen no permitido.");

            try
            {
                var pathCarpeta = Path.Combine(_environment.WebRootPath, "uploads", "manuales");
                if (!Directory.Exists(pathCarpeta)) Directory.CreateDirectory(pathCarpeta);

                // CAPA 5: Sanitización total del nombre
                // No usamos file.FileName directamente para evitar inyecciones de rutas
                var nombreArchivo = $"{Guid.NewGuid()}{extension}";
                var pathCompleto = Path.Combine(pathCarpeta, nombreArchivo);

                using (var stream = new FileStream(pathCompleto, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var url = $"/uploads/manuales/{nombreArchivo}";
                return Ok(new { url });
            }
            catch (Exception ex)
            {
                // Aquí podrías implementar ILogger para registrar el error real
                return StatusCode(500, "Error interno al procesar la imagen.");
            }
        }
    }
}