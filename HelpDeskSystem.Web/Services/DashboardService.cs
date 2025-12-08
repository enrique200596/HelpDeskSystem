using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class DashboardService : IDashboardService
    {
        // Usamos la fábrica en lugar del contexto directo para evitar conflictos de hilos
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public DashboardService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<DashboardDto> ObtenerMetricasAsync(Guid usuarioId, string rol)
        {
            // 1. Crear un contexto nuevo y ligero SOLO para esta operación
            using var context = await _dbFactory.CreateDbContextAsync();

            // 2. Definir la consulta base (IMPORTANTE: Esto faltaba en tu código anterior)
            var query = context.Tickets.AsQueryable();

            // 3. FILTROS DE SEGURIDAD
            if (rol == "Asesor")
            {
                // El asesor SOLO ve sus tickets asignados
                query = query.Where(t => t.AsesorId == usuarioId);
            }
            else if (rol == "Usuario")
            {
                // El usuario SOLO ve los tickets que él creó
                query = query.Where(t => t.UsuarioId == usuarioId);
            }
            // Si es Admin, no entra en los if y la 'query' se queda trayendo TODO.

            // 4. CÁLCULOS (Ejecutamos la query filtrada)
            var total = await query.CountAsync();
            var resueltos = await query.CountAsync(t => t.Estado == EstadoTicket.Resuelto);
            var pendientes = total - resueltos;

            // 5. Promedio de satisfacción
            // Nota: Hacemos la proyección Select para traer solo el dato necesario y optimizar
            var calificaciones = await query
                .Where(t => t.SatisfaccionUsuario != null)
                .Select(t => t.SatisfaccionUsuario!.Value)
                .ToListAsync();

            double promedioSatisfaccion = 0;
            if (calificaciones.Any())
            {
                promedioSatisfaccion = calificaciones.Average();
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