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
            // El filtro global en AppDbContext ya se encarga de 'IsActive == true'
            return await context.Usuarios
                .Where(u => u.Rol == RolUsuario.Asesor)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Usuario>> ObtenerTodosLosUsuariosAsync()
        {
            using var context = _dbFactory.CreateDbContext();
            // IgnoreQueryFilters() permite al Administrador ver y reactivar usuarios inactivos
            return await context.Usuarios
                .IgnoreQueryFilters()
                .OrderBy(u => u.Nombre)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> CrearUsuarioAsync(Usuario usuario, string password)
        {
            using var context = _dbFactory.CreateDbContext();

            // Verificamos si el correo ya existe, incluso entre inactivos, para evitar duplicados
            if (await context.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == usuario.Email))
                return false;

            // Hasheo de contraseña profesional
            usuario.Password = BCrypt.Net.BCrypt.HashPassword(password);

            if (usuario.Id == Guid.Empty)
                usuario.Id = Guid.NewGuid();

            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<Usuario?> ObtenerUsuarioConCategoriasAsync(Guid userId)
        {
            using var context = _dbFactory.CreateDbContext();
            // Usamos IgnoreQueryFilters por si se necesita gestionar un usuario actualmente inactivo
            return await context.Usuarios
                .IgnoreQueryFilters()
                .Include(u => u.Categorias)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task ActualizarCategoriasUsuarioAsync(Guid userId, List<int> idsCategoriasSeleccionadas)
        {
            using var context = _dbFactory.CreateDbContext();

            var usuario = await context.Usuarios
                .IgnoreQueryFilters()
                .Include(u => u.Categorias)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario != null)
            {
                usuario.Categorias.Clear();

                var nuevasCategorias = await context.Categorias
                    .Where(c => idsCategoriasSeleccionadas.Contains(c.Id))
                    .ToListAsync();

                foreach (var cat in nuevasCategorias)
                {
                    usuario.Categorias.Add(cat);
                }

                await context.SaveChangesAsync();
            }
        }

        public async Task<bool> ActualizarUsuarioAsync(Usuario usuarioModificado)
        {
            using var context = _dbFactory.CreateDbContext();

            // IgnoreQueryFilters() es vital para encontrar y reactivar usuarios con IsActive = false
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

        public async Task<bool> CambiarPasswordAsync(Guid userId, string nuevaPasswordPlana)
        {
            using var context = _dbFactory.CreateDbContext();
            var usuarioDb = await context.Usuarios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuarioDb == null) return false;

            usuarioDb.Password = BCrypt.Net.BCrypt.HashPassword(nuevaPasswordPlana);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<Usuario?> ObtenerPorEmailAsync(string email)
        {
            using var context = _dbFactory.CreateDbContext();
            // Para el Login NO usamos IgnoreQueryFilters: si el usuario está inactivo, el sistema no lo encuentra
            return await context.Usuarios
                .Include(u => u.Categorias)
                .FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}