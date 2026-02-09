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
            if (asesorId.HasValue)
                query = query.Where(t => t.AsesorId == asesorId.Value);

            // Aseguramos que las fechas se comparen correctamente en formato UTC
            if (desde.HasValue)
                query = query.Where(t => t.FechaCreacion >= desde.Value.Date.ToUniversalTime());

            if (hasta.HasValue)
            {
                // Incluimos todo el día hasta el último milisegundo
                var finDelDia = hasta.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                query = query.Where(t => t.FechaCreacion <= finDelDia);
            }
            return query;
        }

        public async Task<string> ObtenerTiempoPromedio(Guid? asesorId = null, DateTime? desde = null, DateTime? hasta = null)
        {
            using var context = _dbFactory.CreateDbContext();
            var query = context.Tickets.AsNoTracking();

            query = AplicarFiltro(query, asesorId, desde, hasta);

            // OPTIMIZACIÓN 10/10: Cálculo de promedio directamente en el motor SQL
            // Evitamos traer miles de registros a la memoria de la aplicación
            var promedioMinutos = await query
                .Where(t => t.Estado == EstadoTicket.Resuelto && t.FechaCierre != null)
                .Select(t => EF.Functions.DateDiffMinute(t.FechaCreacion, t.FechaCierre!.Value))
                .Cast<double?>() // Permite manejar resultados nulos si no hay datos
                .AverageAsync();

            if (promedioMinutos == null || promedioMinutos == 0)
                return "0.0 horas";

            double promedioHoras = promedioMinutos.Value / 60.0;
            return $"{promedioHoras:F1} horas";
        }

        public async Task<List<ReporteDato>> ObtenerTicketsPorCategoria(Guid? asesorId = null, DateTime? desde = null, DateTime? hasta = null)
        {
            using var context = _dbFactory.CreateDbContext();
            var query = context.Tickets.AsNoTracking();

            query = AplicarFiltro(query, asesorId, desde, hasta);
            query = query.Where(t => t.Estado == EstadoTicket.Resuelto);

            // Agrupación eficiente en servidor por relación de navegación
            var datos = await query
                .GroupBy(t => t.CategoriaId)
                .Select(g => new ReporteDato
                {
                    // Obtenemos el nombre de la categoría o un valor por defecto
                    Etiqueta = g.Select(t => t.Categoria.Nombre).FirstOrDefault() ?? "Sin Categoría",
                    Valor = g.Count()
                })
                .ToListAsync();

            // Cálculo de porcentajes en memoria (después de la agregación de SQL)
            int total = datos.Sum(d => d.Valor);
            if (total > 0)
            {
                foreach (var d in datos)
                    d.Porcentaje = (d.Valor * 100) / total;
            }

            return datos.OrderByDescending(d => d.Valor).ToListAsync().Result; // Ordenamos por volumen
        }

        public async Task<List<Ticket>> ObtenerDetalleTickets(Guid? asesorId = null, DateTime? desde = null, DateTime? hasta = null)
        {
            using var context = _dbFactory.CreateDbContext();
            var query = context.Tickets
                .AsNoTracking()
                .Include(t => t.Usuario)
                .Include(t => t.Asesor)
                .Include(t => t.Categoria);

            query = AplicarFiltro(query, asesorId, desde, hasta);

            // Un reporte de detalle suele enfocarse en lo resuelto para auditoría
            query = query.Where(t => t.Estado == EstadoTicket.Resuelto);

            return await query
                .OrderByDescending(t => t.FechaCierre)
                .ToListAsync();
        }
    }
}