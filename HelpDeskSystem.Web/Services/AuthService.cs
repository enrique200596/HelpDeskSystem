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

            bool verificado = false;

            // --- CORRECCIÓN AQUÍ ---
            try
            {
                // Intentamos verificar como hash seguro
                verificado = BCrypt.Net.BCrypt.Verify(password, usuario.Password);
            }
            catch
            {
                // Si BCrypt falla (porque la contraseña es "1234" y no un hash),
                // capturamos el error y continuamos para verificarla como texto plano.
                verificado = false;
            }
            // -----------------------

            // Parche de compatibilidad para usuarios antiguos ("1234")
            if (!verificado && usuario.Password == password)
            {
                verificado = true;
            }

            return verificado ? usuario : null;
        }
    }
}