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

            // 1. Consulta base (No Tracking para velocidad)
            var query = context.Tickets.AsNoTracking().AsQueryable();

            // 2. Aplicar Filtros según Rol
            if (rol == "Administrador")
            {
                // El admin ve todo, no filtramos.
            }
            else if (rol == "Asesor")
            {
                // CORRECCIÓN AQUÍ:
                // No podemos usar context.UsuarioCategoria porque no existe en el DbContext.
                // En su lugar, navegamos desde el Usuario hacia sus Categorías.
                var misCategoriasIds = await context.Usuarios
                    .Where(u => u.Id == userId)
                    .SelectMany(u => u.Categorias.Select(c => c.Id))
                    .ToListAsync();

                // Filtro: Tickets asignados a mí O tickets libres de mis categorías
                query = query.Where(t => t.AsesorId == userId || (t.AsesorId == null && misCategoriasIds.Contains(t.CategoriaId)));
            }
            else // Usuario
            {
                query = query.Where(t => t.UsuarioId == userId);
            }

            // 3. Calcular Métricas
            var metricas = new DashboardDto
            {
                TotalTickets = await query.CountAsync(),
                Resueltos = await query.CountAsync(t => t.Estado == EstadoTicket.Resuelto),
                Pendientes = await query.CountAsync(t => t.Estado != EstadoTicket.Resuelto),
            };

            // 4. Calcular Satisfacción (solo de tickets resueltos y calificados)
            // Nota: .Average() falla si la lista está vacía, por eso validamos con .Any()
            var calificaciones = await query
                .Where(t => t.Estado == EstadoTicket.Resuelto && t.SatisfaccionUsuario != null)
                .Select(t => (double)t.SatisfaccionUsuario!)
                .ToListAsync();

            metricas.PromedioSatisfaccion = calificaciones.Any() ? calificaciones.Average() : 0;

            return metricas;
        }
    }
}