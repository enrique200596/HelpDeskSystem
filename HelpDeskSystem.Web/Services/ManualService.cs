using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class ManualService : IManualService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public ManualService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<Manual>> ObtenerTodosAsync()
        {
            using var context = _dbFactory.CreateDbContext();
            // Incluimos al Autor para mostrar quién lo escribió
            return await context.Manuales
                .Include(m => m.Autor)
                .OrderByDescending(m => m.UltimaActualizacion ?? m.FechaCreacion)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Manual?> ObtenerPorIdAsync(int id)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Manuales
                .Include(m => m.Autor)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task GuardarManualAsync(Manual manual)
        {
            using var context = _dbFactory.CreateDbContext();

            if (manual.Id == 0)
            {
                // Es un nuevo manual
                manual.FechaCreacion = DateTime.Now;
                context.Manuales.Add(manual);
            }
            else
            {
                // Es una edición
                var manualExistente = await context.Manuales.FindAsync(manual.Id);
                if (manualExistente != null)
                {
                    manualExistente.Titulo = manual.Titulo;
                    manualExistente.ContenidoHTML = manual.ContenidoHTML;
                    manualExistente.UltimaActualizacion = DateTime.Now;
                    // Opcional: Actualizar AutorId si quieres reflejar el último que editó
                    manualExistente.AutorId = manual.AutorId;
                }
            }
            await context.SaveChangesAsync();
        }

        public async Task EliminarManualAsync(int id)
        {
            using var context = _dbFactory.CreateDbContext();
            var manual = await context.Manuales.FindAsync(id);
            if (manual != null)
            {
                context.Manuales.Remove(manual);
                await context.SaveChangesAsync();
            }
        }
    }
}