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
    }
}