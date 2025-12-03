using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        // --- ESTE ES EL CAMBIO IMPORTANTE: ACEPTAR ID Y ROL ---
        public async Task<DashboardDto> ObtenerMetricasAsync(Guid usuarioId, string rol)
        {
            var query = _context.Tickets.AsQueryable();

            // 1. FILTROS DE SEGURIDAD
            if (rol == "Asesor")
            {
                // El asesor SOLO ve sus tickets
                query = query.Where(t => t.AsesorId == usuarioId);
            }
            else if (rol == "Usuario")
            {
                // El usuario SOLO ve sus tickets
                query = query.Where(t => t.UsuarioId == usuarioId);
            }
            // Si es Admin, no entra en los if y ve TODO.

            // 2. CÁLCULOS
            var total = await query.CountAsync();
            var resueltos = await query.CountAsync(t => t.Estado == EstadoTicket.Resuelto);
            var pendientes = total - resueltos;

            // Promedio de satisfacción
            var ticketsCalificados = await query
                .Where(t => t.SatisfaccionUsuario != null)
                .ToListAsync();

            double promedioSatisfaccion = 0;
            if (ticketsCalificados.Any())
            {
                promedioSatisfaccion = ticketsCalificados.Average(t => t.SatisfaccionUsuario!.Value);
            }

            return new DashboardDto
            {
                TotalTickets = total,
                Resueltos = resueltos,
                Pendientes = pendientes,
                PromedioSatisfaccion = promedioSatisfaccion
            };
        }
    }

    public class DashboardDto
    {
        public int TotalTickets { get; set; }
        public int Resueltos { get; set; }
        public int Pendientes { get; set; }
        public double PromedioSatisfaccion { get; set; }
    }
}