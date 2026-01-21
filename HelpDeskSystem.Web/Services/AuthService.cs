using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // NECESARIO PARA AUDITORÍA

namespace HelpDeskSystem.Web.Services
{
    public class AuthService : IAuthService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<AuthService> _logger; // CAPA DE AUDITORÍA

        public AuthService(IDbContextFactory<AppDbContext> dbFactory, ILogger<AuthService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<(Usuario? Usuario, string MensajeError)> LoginAsync(string email, string password)
        {
            _logger.LogInformation("Intento de inicio de sesión para el correo: {Email}", email);

            try
            {
                using var context = _dbFactory.CreateDbContext();

                // 1. Buscamos al usuario (Sin tracking para mejorar rendimiento)
                var usuario = await context.Usuarios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (usuario == null)
                {
                    _logger.LogWarning("Login fallido: El correo {Email} no existe en la base de datos.", email);
                    return (null, "Usuario o contraseña incorrectos."); // Mensaje genérico por seguridad
                }

                // 2. Verificamos si el usuario está activo
                if (!usuario.IsActive)
                {
                    _logger.LogWarning("Acceso denegado: El usuario {Email} está deshabilitado.", email);
                    return (null, "Su cuenta está deshabilitada. Contacte al administrador.");
                }

                // 3. Verificación de Contraseña (BCrypt)
                bool passwordValida = false;
                try
                {
                    passwordValida = BCrypt.Net.BCrypt.Verify(password, usuario.Password);
                }
                catch (Exception ex)
                {
                    // Registramos el error técnico pero no lo mostramos al usuario
                    _logger.LogCritical(ex, "Error crítico en el motor de cifrado BCrypt para el usuario {Email}.", email);
                    return (null, "Error interno en la validación de seguridad.");
                }

                if (!passwordValida)
                {
                    _logger.LogWarning("Login fallido: Contraseña incorrecta para {Email}.", email);
                    return (null, "Usuario o contraseña incorrectos.");
                }

                // 4. Éxito
                _logger.LogInformation("Login exitoso: Usuario {Nombre} ({Email}) ha ingresado al sistema.", usuario.Nombre, email);
                return (usuario, "");
            }
            catch (Exception ex)
            {
                // Resiliencia ante caídas de la Base de Datos
                _logger.LogError(ex, "Error no controlado durante el Login para {Email}.", email);
                return (null, "El servicio no está disponible temporalmente.");
            }
        }
    }
}