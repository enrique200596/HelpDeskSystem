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

        public async Task CrearUsuario(Usuario nuevoUsuario, string passwordPlano)
        {
            // ENCRIPTAR ANTES DE GUARDAR
            nuevoUsuario.Password = BCrypt.Net.BCrypt.HashPassword(passwordPlano);

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();
    }
    }
}