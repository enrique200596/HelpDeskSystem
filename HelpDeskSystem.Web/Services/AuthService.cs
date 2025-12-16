using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class AuthService : IAuthService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public AuthService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<(Usuario? Usuario, string MensajeError)> LoginAsync(string email, string password)
        {
            using var context = _dbFactory.CreateDbContext();

            // Usamos AsNoTracking porque solo estamos leyendo para verificar
            var usuario = await context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null)
            {
                return (null, "Usuario no existente.");
            }

            if (!usuario.IsActive)
            {
                return (null, "Su usuario está deshabilitado. Contacte al administrador.");
            }

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

            return (usuario, "");
        }
    }
}