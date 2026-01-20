using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public UsuarioService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<Usuario>> ObtenerAsesoresAsync()
        {
            using var context = _dbFactory.CreateDbContext();
            // Simplificado: El filtro global ya se encarga de 'IsActive == true'
            return await context.Usuarios
                .Where(u => u.Rol == RolUsuario.Asesor)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Usuario>> ObtenerTodosLosUsuariosAsync()
        {
            using var context = _dbFactory.CreateDbContext();
            // IMPORTANTE: En una lista de gestión para el Administrador, 
            // queremos ver TODOS (activos e inactivos) para poder reactivarlos.
            return await context.Usuarios
                .IgnoreQueryFilters() // Saltamos el filtro global para ver inactivos
                .OrderBy(u => u.Nombre)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> ActualizarUsuarioAsync(Usuario usuarioModificado)
        {
            using var context = _dbFactory.CreateDbContext();

            // Usamos IgnoreQueryFilters() porque si el usuario está inactivo, 
            // un FindAsync normal no lo encontraría para editarlo.
            var usuarioDb = await context.Usuarios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == usuarioModificado.Id);

            if (usuarioDb == null) return false;

            usuarioDb.Nombre = usuarioModificado.Nombre;
            usuarioDb.Email = usuarioModificado.Email;
            usuarioDb.Rol = usuarioModificado.Rol;
            usuarioDb.IsActive = usuarioModificado.IsActive;

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<Usuario?> ObtenerPorEmailAsync(string email)
        {
            using var context = _dbFactory.CreateDbContext();
            // Para el Login, el filtro global es perfecto: 
            // si un usuario está inactivo, no podrá entrar porque no lo encontrará.
            return await context.Usuarios
                .Include(u => u.Categorias)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        // ... Los métodos de creación y cambio de password se mantienen similares ...
    }
}