using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class ReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        // --- MÉTODO PRIVADO: APLICA TODOS LOS FILTROS ---
        private IQueryable<Ticket> AplicarFiltro(IQueryable<Ticket> query, Guid? asesorId, DateTime? desde, DateTime? hasta)
        {
            // 1. Filtro por Asesor
            if (asesorId.HasValue)
            {
                query = query.Where(t => t.AsesorId == asesorId.Value);
            }

            // 2. Filtro por Fecha Desde
            if (desde.HasValue)
            {
                query = query.Where(t => t.FechaCreacion >= desde.Value);
            }

            // 3. Filtro por Fecha Hasta (Incluimos el final del día)
            if (hasta.HasValue)
            {
                // Ejemplo: Si eligen 31/12, buscamos hasta 31/12 23:59:59
                var finDelDia = hasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.FechaCreacion <= finDelDia);
            }

            return query;
        }

        // 1. KPI: Tiempo Promedio (Con Filtros de Fecha)
        public async Task<string> ObtenerTiempoPromedio(Guid? asesorId = null, DateTime? desde = null, DateTime? hasta = null)
        {
            var query = _context.Tickets.AsQueryable();

            // Aplicamos filtros de fecha/asesor
            query = AplicarFiltro(query, asesorId, desde, hasta);

            // Solo tickets resueltos y cerrados
            var ticketsCerrados = await query
                .Where(t => t.Estado == EstadoTicket.Resuelto && t.FechaCierre != null)
                .ToListAsync();

            if (!ticketsCerrados.Any()) return "0.0 horas";

            double totalHoras = ticketsCerrados.Sum(t => (t.FechaCierre!.Value - t.FechaCreacion).TotalHours);
            double promedio = totalHoras / ticketsCerrados.Count;

            return $"{promedio:F1} horas";
        }

        // 2. Gráfico: Tickets por Categoría (Con Filtros de Fecha)
        public async Task<List<ReporteDato>> ObtenerTicketsPorCategoria(Guid? asesorId = null, DateTime? desde = null, DateTime? hasta = null)
        {
            var query = _context.Tickets.Include(t => t.Categoria).AsQueryable();

            // Aplicamos filtros
            query = AplicarFiltro(query, asesorId, desde, hasta);

            // Solo contamos tickets resueltos para el reporte final (Opcional: quitar filtro de estado si quieres ver todo lo creado)
            query = query.Where(t => t.Estado == EstadoTicket.Resuelto);

            var datos = await query
                .GroupBy(t => t.Categoria != null ? t.Categoria.Nombre : "Sin Categoría")
                .Select(g => new ReporteDato { Etiqueta = g.Key, Valor = g.Count() })
                .ToListAsync();

            int total = datos.Sum(d => d.Valor);
            foreach (var d in datos) d.Porcentaje = total > 0 ? (d.Valor * 100) / total : 0;

            return datos;
        }
    }

    public class ReporteDato
    {
        public string Etiqueta { get; set; } = "";
        public int Valor { get; set; }
        public int Porcentaje { get; set; }
    }
}