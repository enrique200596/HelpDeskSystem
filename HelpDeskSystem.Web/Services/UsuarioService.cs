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

        // --- MÉTODOS DE CONSULTA ---

        public async Task<List<Usuario>> ObtenerAsesoresAsync()
        {
            using var context = _dbFactory.CreateDbContext();
            // El filtro global en AppDbContext ya excluye a los inactivos por defecto
            return await context.Usuarios
                .Where(u => u.Rol == RolUsuario.Asesor)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Usuario>> ObtenerTodosLosUsuariosAsync()
        {
            using var context = _dbFactory.CreateDbContext();
            // Usamos IgnoreQueryFilters para que el Admin pueda ver y reactivar cuentas deshabilitadas
            return await context.Usuarios
                .IgnoreQueryFilters()
                .OrderBy(u => u.Nombre)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Usuario?> ObtenerUsuarioConCategoriasAsync(Guid userId)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Usuarios
                .IgnoreQueryFilters()
                .Include(u => u.Categorias)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<Usuario?> ObtenerPorEmailAsync(string email)
        {
            using var context = _dbFactory.CreateDbContext();
            // Para el login no ignoramos filtros: si la cuenta está inactiva, no se encuentra
            return await context.Usuarios
                .Include(u => u.Categorias)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        // --- MÉTODOS DE ESCRITURA Y GESTIÓN ---

        public async Task<bool> CrearUsuarioAsync(Usuario usuario, string password)
        {
            using var context = _dbFactory.CreateDbContext();

            // Validación de duplicados incluso en registros inactivos
            if (await context.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == usuario.Email))
                return false;

            // Seguridad: Hasheo de contraseña antes de persistir
            usuario.Password = BCrypt.Net.BCrypt.HashPassword(password);

            if (usuario.Id == Guid.Empty)
                usuario.Id = Guid.NewGuid();

            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActualizarUsuarioAsync(Usuario usuarioModificado)
        {
            using var context = _dbFactory.CreateDbContext();

            // Buscamos el registro existente ignorando filtros para permitir reactivaciones
            var usuarioDb = await context.Usuarios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == usuarioModificado.Id);

            if (usuarioDb == null) return false;

            // Validación de correo: Evita que al editar se asigne un email ya usado por otro
            if (usuarioDb.Email != usuarioModificado.Email &&
                await context.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == usuarioModificado.Email))
                return false;

            usuarioDb.Nombre = usuarioModificado.Nombre;
            usuarioDb.Email = usuarioModificado.Email;
            usuarioDb.Rol = usuarioModificado.Rol;
            usuarioDb.IsActive = usuarioModificado.IsActive;

            await context.SaveChangesAsync();
            return true;
        }

        public async Task ActualizarCategoriasUsuarioAsync(Guid userId, List<int> idsCategoriasSeleccionadas)
        {
            using var context = _dbFactory.CreateDbContext();

            // Cargamos el usuario con sus categorías actuales
            var usuario = await context.Usuarios
                .IgnoreQueryFilters()
                .Include(u => u.Categorias)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario != null)
            {
                // CORRECCIÓN: Sincronización segura de la relación Muchos a Muchos
                // Limpiamos las relaciones actuales
                usuario.Categorias.Clear();

                // Obtenemos las nuevas entidades de categorías desde la base de datos
                var nuevasCategorias = await context.Categorias
                    .Where(c => idsCategoriasSeleccionadas.Contains(c.Id))
                    .ToListAsync();

                // Asignamos las nuevas relaciones
                foreach (var cat in nuevasCategorias)
                {
                    usuario.Categorias.Add(cat);
                }

                await context.SaveChangesAsync();
            }
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
    }
}