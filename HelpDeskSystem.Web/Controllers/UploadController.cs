using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public UploadController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost("imagen")]
        public async Task<IActionResult> SubirImagen(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se subió ninguna imagen.");

            // Crear carpeta si no existe
            var pathCarpeta = Path.Combine(_environment.WebRootPath, "uploads", "manuales");
            if (!Directory.Exists(pathCarpeta)) Directory.CreateDirectory(pathCarpeta);

            // Generar nombre único
            var nombreArchivo = $"{Guid.NewGuid()}_{file.FileName}";
            var pathCompleto = Path.Combine(pathCarpeta, nombreArchivo);

            // Guardar
            using (var stream = new FileStream(pathCompleto, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Devolver la URL pública
            var url = $"/uploads/manuales/{nombreArchivo}";
            return Ok(new { url });
        }
    }
}