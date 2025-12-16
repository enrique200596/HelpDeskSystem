using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class ReportService : IReportService
    {
        // CAMBIO: Usar Factory
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public ReportService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        // Método helper estático o privado para reutilizar lógica de filtros
        // Nota: Recibe IQueryable, funciona siempre que se consuma dentro del mismo contexto
        private IQueryable<Ticket> AplicarFiltro(IQueryable<Ticket> query, Guid? asesorId, DateTime? desde, DateTime? hasta)
        {
            if (asesorId.HasValue) query = query.Where(t => t.AsesorId == asesorId.Value);
            if (desde.HasValue) query = query.Where(t => t.FechaCreacion >= desde.Value);
            if (hasta.HasValue)
            {
                var finDelDia = hasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.FechaCreacion <= finDelDia);
            }
            return query;
        }

        public async Task<string> ObtenerTiempoPromedio(Guid? asesorId = null, DateTime? desde = null, DateTime? hasta = null)
        {
            using var context = _dbFactory.CreateDbContext();
            var query = context.Tickets.AsNoTracking().AsQueryable();

            query = AplicarFiltro(query, asesorId, desde, hasta);

            var ticketsCerrados = await query
                .Where(t => t.Estado == EstadoTicket.Resuelto && t.FechaCierre != null)
                .Select(t => new { t.FechaCreacion, t.FechaCierre }) // Proyección para eficiencia
                .ToListAsync();

            if (!ticketsCerrados.Any()) return "0.0 horas";

            double totalHoras = ticketsCerrados.Sum(t => (t.FechaCierre!.Value - t.FechaCreacion).TotalHours);
            double promedio = totalHoras / ticketsCerrados.Count;

            return $"{promedio:F1} horas";
        }

        public async Task<List<ReporteDato>> ObtenerTicketsPorCategoria(Guid? asesorId = null, DateTime? desde = null, DateTime? hasta = null)
        {
            using var context = _dbFactory.CreateDbContext();
            var query = context.Tickets.Include(t => t.Categoria).AsNoTracking().AsQueryable();

            query = AplicarFiltro(query, asesorId, desde, hasta);
            query = query.Where(t => t.Estado == EstadoTicket.Resuelto);

            var datos = await query
                .GroupBy(t => t.Categoria != null ? t.Categoria.Nombre : "Sin Categoría")
                .Select(g => new ReporteDato { Etiqueta = g.Key, Valor = g.Count() })
                .ToListAsync();

            int total = datos.Sum(d => d.Valor);
            foreach (var d in datos) d.Porcentaje = total > 0 ? (d.Valor * 100) / total : 0;

            return datos;
        }

        public async Task<List<Ticket>> ObtenerDetalleTickets(Guid? asesorId = null, DateTime? desde = null, DateTime? hasta = null)
        {
            using var context = _dbFactory.CreateDbContext();
            var query = context.Tickets
                .Include(t => t.Usuario)
                .Include(t => t.Asesor)
                .Include(t => t.Categoria)
                .AsNoTracking()
                .AsQueryable();

            query = AplicarFiltro(query, asesorId, desde, hasta);
            query = query.Where(t => t.Estado == EstadoTicket.Resuelto);

            return await query.OrderByDescending(t => t.FechaCierre).ToListAsync();
        }
    }
}