using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly AppDbContext _context;

        public UsuarioService(AppDbContext context)
        {
            _context = context;
        }

        // Método para traer SOLO a los asesores activos
        public async Task<List<Usuario>> ObtenerAsesoresAsync()
        {
            return await _context.Usuarios
                .Where(u => u.Rol == RolUsuario.Asesor && u.IsActive)
                .ToListAsync();
        }

        public async Task<bool> CrearUsuarioAsync(Usuario usuario, string password)
        {
            // Validar si el correo ya existe
            if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
                return false;

            // Encriptar contraseña
            usuario.Password = BCrypt.Net.BCrypt.HashPassword(password);

            // Asignar ID si no viene
            if (usuario.Id == Guid.Empty) usuario.Id = Guid.NewGuid();

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Usuario?> ObtenerUsuarioConCategoriasAsync(Guid userId)
        {
            return await _context.Usuarios
                .Include(u => u.Categorias) // Importante: Cargar la relación
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task ActualizarCategoriasUsuarioAsync(Guid userId, List<int> idsCategoriasSeleccionadas)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Categorias)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario != null)
            {
                // 1. Limpiar las categorías actuales
                usuario.Categorias.Clear();

                // 2. Buscar las nuevas categorías en BD
                var nuevasCategorias = await _context.Categorias
                    .Where(c => idsCategoriasSeleccionadas.Contains(c.Id))
                    .ToListAsync();

                // 3. Agregarlas al usuario
                foreach (var cat in nuevasCategorias)
                {
                    usuario.Categorias.Add(cat);
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Usuario>> ObtenerTodosLosUsuariosAsync()
        {
            // Retornamos todos ordenados por nombre
            return await _context.Usuarios.OrderBy(u => u.Nombre).ToListAsync();
        }

        public async Task<bool> ActualizarUsuarioAsync(Usuario usuarioModificado)
        {
            var usuarioDb = await _context.Usuarios.FindAsync(usuarioModificado.Id);
            if (usuarioDb == null) return false;

            // Actualizamos solo los campos permitidos (NO la contraseña aquí)
            usuarioDb.Nombre = usuarioModificado.Nombre;
            usuarioDb.Email = usuarioModificado.Email;
            usuarioDb.Rol = usuarioModificado.Rol;
            usuarioDb.IsActive = usuarioModificado.IsActive; // Para dar de baja/alta

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CambiarPasswordAsync(Guid userId, string nuevaPasswordPlana)
        {
            var usuarioDb = await _context.Usuarios.FindAsync(userId);
            if (usuarioDb == null) return false;

            // Encriptamos y guardamos
            usuarioDb.Password = BCrypt.Net.BCrypt.HashPassword(nuevaPasswordPlana);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}