using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class UsuarioService : IUsuarioService
    {
        // CAMBIO: Usar Factory
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public UsuarioService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<Usuario>> ObtenerAsesoresAsync()
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Usuarios
                .Where(u => u.Rol == RolUsuario.Asesor && u.IsActive)
                .AsNoTracking() // Recomendado para lectura
                .ToListAsync();
        }

        public async Task<bool> CrearUsuarioAsync(Usuario usuario, string password)
        {
            using var context = _dbFactory.CreateDbContext();

            if (await context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
                return false;

            usuario.Password = BCrypt.Net.BCrypt.HashPassword(password);
            if (usuario.Id == Guid.Empty) usuario.Id = Guid.NewGuid();

            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<Usuario?> ObtenerUsuarioConCategoriasAsync(Guid userId)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Usuarios
                .Include(u => u.Categorias)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task ActualizarCategoriasUsuarioAsync(Guid userId, List<int> idsCategoriasSeleccionadas)
        {
            using var context = _dbFactory.CreateDbContext();

            // Aquí NO usamos AsNoTracking porque vamos a modificar la entidad
            var usuario = await context.Usuarios
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

        public async Task<List<Usuario>> ObtenerTodosLosUsuariosAsync()
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Usuarios
                .OrderBy(u => u.Nombre)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> ActualizarUsuarioAsync(Usuario usuarioModificado)
        {
            using var context = _dbFactory.CreateDbContext();
            var usuarioDb = await context.Usuarios.FindAsync(usuarioModificado.Id);

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
            var usuarioDb = await context.Usuarios.FindAsync(userId);

            if (usuarioDb == null) return false;

            usuarioDb.Password = BCrypt.Net.BCrypt.HashPassword(nuevaPasswordPlana);
            await context.SaveChangesAsync();
            return true;
        }
    }
}