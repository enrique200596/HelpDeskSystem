using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class ReportService : IReportService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public ReportService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        private IQueryable<Ticket> AplicarFiltro(IQueryable<Ticket> query, Guid? asesorId, DateTime? desde, DateTime? hasta)
        {
            if (asesorId.HasValue) query = query.Where(t => t.AsesorId == asesorId.Value);

            // Aseguramos que los filtros de fecha traten las entradas como UTC
            if (desde.HasValue) query = query.Where(t => t.FechaCreacion >= desde.Value.ToUniversalTime());
            if (hasta.HasValue)
            {
                var finDelDia = hasta.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                query = query.Where(t => t.FechaCreacion <= finDelDia);
            }
            return query;
        }

        public async Task<string> ObtenerTiempoPromedio(Guid? asesorId = null, DateTime? desde = null, DateTime? hasta = null)
        {
            using var context = _dbFactory.CreateDbContext();
            var query = context.Tickets.AsNoTracking().AsQueryable();

            query = AplicarFiltro(query, asesorId, desde, hasta);

            // OPTIMIZACIÓN 10/10: Calculamos la diferencia de tiempo en el servidor de BD
            // Usamos una proyección para obtener solo los minutos totales de resolución
            var minutosResolucion = await query
                .Where(t => t.Estado == EstadoTicket.Resuelto && t.FechaCierre != null)
                .Select(t => EF.Functions.DateDiffMinute(t.FechaCreacion, t.FechaCierre!.Value))
                .ToListAsync();

            if (!minutosResolucion.Any()) return "0.0 horas";

            double promedioHoras = minutosResolucion.Average() / 60.0;

            return $"{promedioHoras:F1} horas";
        }

        public async Task<List<ReporteDato>> ObtenerTicketsPorCategoria(Guid? asesorId = null, DateTime? desde = null, DateTime? hasta = null)
        {
            using var context = _dbFactory.CreateDbContext();
            var query = context.Tickets.AsNoTracking().AsQueryable();

            query = AplicarFiltro(query, asesorId, desde, hasta);
            query = query.Where(t => t.Estado == EstadoTicket.Resuelto);

            // Agrupamos directamente en la base de datos por el nombre de la categoría
            var datos = await query
                .GroupBy(t => t.Categoria.Nombre)
                .Select(g => new ReporteDato
                {
                    Etiqueta = g.Key ?? "Sin Categoría",
                    Valor = g.Count()
                })
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
                .AsNoTracking();

            query = AplicarFiltro(query, asesorId, desde, hasta);
            query = query.Where(t => t.Estado == EstadoTicket.Resuelto);

            return await query.OrderByDescending(t => t.FechaCierre).ToListAsync();
        }
    }
}