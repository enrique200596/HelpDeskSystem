using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
// Asegúrate de haber instalado: BCrypt.Net-Next

namespace HelpDeskSystem.Web.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> LoginAsync(string email, string password)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (usuario == null) return null;

            // Verificación estricta con BCrypt
            bool verificado = false;
            try
            {
                verificado = BCrypt.Net.BCrypt.Verify(password, usuario.Password);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                verificado = false; // El hash en BD no es válido
            }

            return verificado ? usuario : null;
        }
    }
}