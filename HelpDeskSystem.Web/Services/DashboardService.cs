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
            var query = context.Tickets.AsNoTracking().AsQueryable();

            // Aplicar Filtros según Rol (Manteniendo tu lógica corregida)
            if (rol == "Asesor")
            {
                var misCategoriasIds = await context.Usuarios
                    .Where(u => u.Id == userId)
                    .SelectMany(u => u.Categorias.Select(c => c.Id))
                    .ToListAsync();

                query = query.Where(t => t.AsesorId == userId || (t.AsesorId == null && misCategoriasIds.Contains(t.CategoriaId)));
            }
            else if (rol == "Usuario")
            {
                query = query.Where(t => t.UsuarioId == userId);
            }

            // OPTIMIZACIÓN 10/10: Ejecutamos los conteos de forma eficiente
            var total = await query.CountAsync();
            var resueltos = await query.CountAsync(t => t.Estado == EstadoTicket.Resuelto);

            // OPTIMIZACIÓN 10/10: Cálculo del promedio DIRECTO en SQL
            // Evitamos traer la lista de calificaciones a la memoria
            double promedioSatisfaccion = 0;
            var queryCalificaciones = query.Where(t => t.Estado == EstadoTicket.Resuelto && t.SatisfaccionUsuario != null);

            if (await queryCalificaciones.AnyAsync())
            {
                promedioSatisfaccion = await queryCalificaciones.AverageAsync(t => (double)t.SatisfaccionUsuario!);
            }

            return new DashboardDto
            {
                TotalTickets = total,
                Resueltos = resueltos,
                Pendientes = total - resueltos,
                PromedioSatisfaccion = promedioSatisfaccion
            };
        }
    }
}