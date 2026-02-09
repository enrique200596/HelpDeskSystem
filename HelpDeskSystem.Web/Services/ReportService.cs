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

            // CORRECCIÓN: Se utiliza (int?) en la proyección para asegurar la traducción a SQL Server.
            // EF Core traduce AverageAsync() sobre tipos anulables devolviendo double?, lo que 
            // evita errores de "secuencia vacía" si no hay tickets resueltos en el periodo.
            var promedioMinutos = await query
                .Where(t => t.Estado == EstadoTicket.Resuelto && t.FechaCierre != null)
                .Select(t => (int?)EF.Functions.DateDiffMinute(t.FechaCreacion, t.FechaCierre!.Value))
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

            var datos = await query
                .GroupBy(t => t.CategoriaId)
                .Select(g => new ReporteDato
                {
                    // EF Core traducirá esto a un JOIN y COALESCE en SQL.
                    Etiqueta = g.Select(t => t.Categoria!.Nombre).FirstOrDefault() ?? "Sin Categoría",
                    Valor = g.Count()
                })
                .ToListAsync();

            int total = datos.Sum(d => d.Valor);
            if (total > 0)
            {
                foreach (var d in datos)
                    d.Porcentaje = (d.Valor * 100) / total;
            }

            return datos.OrderByDescending(d => d.Valor).ToList();
        }

        public async Task<List<Ticket>> ObtenerDetalleTickets(Guid? asesorId = null, DateTime? desde = null, DateTime? hasta = null)
        {
            using var context = _dbFactory.CreateDbContext();

            IQueryable<Ticket> query = context.Tickets
                .AsNoTracking()
                .Include(t => t.Usuario)
                .Include(t => t.Asesor)
                .Include(t => t.Categoria);

            query = AplicarFiltro(query, asesorId, desde, hasta);

            query = query.Where(t => t.Estado == EstadoTicket.Resuelto);

            return await query
                .OrderByDescending(t => t.FechaCierre)
                .ToListAsync();
        }
    }

}