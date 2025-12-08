using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(Usuario? Usuario, string MensajeError)> LoginAsync(string email, string password)
        {
            // 1. Buscamos al usuario SOLO por correo (sin filtrar IsActive aún)
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == email);

            // CASO A: Usuario no existe en la BD
            if (usuario == null)
            {
                return (null, "Usuario no existente.");
            }

            // CASO B: Usuario existe, pero está deshabilitado
            if (!usuario.IsActive)
            {
                return (null, "Su usuario está deshabilitado. Contacte al administrador.");
            }

            // CASO C: Verificar contraseña
            bool passwordValida = false;
            try
            {
                passwordValida = BCrypt.Net.BCrypt.Verify(password, usuario.Password);
            }
            catch
            {
                passwordValida = false;
            }

            if (!passwordValida)
            {
                return (null, "Contraseña errónea.");
            }

            // ÉXITO: Retornamos el usuario y mensaje vacío
            return (usuario, "");
        }
    }
}