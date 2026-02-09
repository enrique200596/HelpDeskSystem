using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public DashboardService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<DashboardDto> ObtenerMetricasAsync(Guid userId, string rol)
        {
            using var context = _dbFactory.CreateDbContext();

            // 1. Definimos la base de la consulta (Sin tracking para máxima velocidad)
            var query = context.Tickets.AsNoTracking();

            // 2. Aplicación de Filtros de Seguridad por Rol (Consistente con TicketService)
            if (rol == RolUsuario.Asesor.ToString())
            {
                var misCategoriasIds = await context.Usuarios
                    .Where(u => u.Id == userId)
                    .SelectMany(u => u.Categorias.Select(c => c.Id))
                    .ToListAsync();

                query = query.Where(t => t.AsesorId == userId ||
                                   (t.AsesorId == null && misCategoriasIds.Contains(t.CategoriaId)));
            }
            else if (rol == RolUsuario.Usuario.ToString())
            {
                query = query.Where(t => t.UsuarioId == userId);
            }
            // Los administradores no entran en los IFs, por lo que ven la query completa

            // 3. OPTIMIZACIÓN 10/10: Agregación en un solo viaje a la base de datos
            // Usamos una proyección para que SQL Server calcule todo de una vez
            var estadisticas = await query
                .GroupBy(t => 1) // Agrupamos todo en un solo conjunto
                .Select(g => new
                {
                    Total = g.Count(),
                    Resueltos = g.Count(t => t.Estado == EstadoTicket.Resuelto),
                    // Promedio de satisfacción solo para los resueltos que tienen calificación
                    SumaSatisfaccion = g.Where(t => t.Estado == EstadoTicket.Resuelto && t.SatisfaccionUsuario != null)
                                        .Sum(t => (double?)t.SatisfaccionUsuario) ?? 0,
                    ConCalificacion = g.Count(t => t.Estado == EstadoTicket.Resuelto && t.SatisfaccionUsuario != null)
                })
                .FirstOrDefaultAsync();

            // 4. Mapeo al DTO y manejo de casos vacíos
            if (estadisticas == null)
            {
                return new DashboardDto();
            }

            return new DashboardDto
            {
                TotalTickets = estadisticas.Total,
                Resueltos = estadisticas.Resueltos,
                Pendientes = estadisticas.Total - estadisticas.Resueltos,
                PromedioSatisfaccion = estadisticas.ConCalificacion > 0
                    ? estadisticas.SumaSatisfaccion / estadisticas.ConCalificacion
                    : 0
            };
        }
    }
}