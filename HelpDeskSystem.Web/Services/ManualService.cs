using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HelpDeskSystem.Web.Services
{
    public class ManualService : IManualService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<ManualService> _logger; // Inyección para resiliencia

        public ManualService(IDbContextFactory<AppDbContext> dbFactory, ILogger<ManualService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<List<Manual>> ObtenerTodosAsync(string? rolUsuario = null)
        {
            using var context = _dbFactory.CreateDbContext();

            var query = context.Manuales
                .Include(m => m.Autor)
                .AsNoTracking()
                .AsQueryable();

            // Si no es admin, filtramos por visibilidad y estado activo
            // Nota: IsDeleted ya lo filtra AppDbContext automáticamente
            if (rolUsuario != nameof(RolUsuario.Administrador))
            {
                query = query.Where(m => m.IsActive &&
                    (string.IsNullOrEmpty(m.RolesVisibles) ||
                     (!string.IsNullOrEmpty(rolUsuario) && m.RolesVisibles.Contains(rolUsuario))));
            }
            else
            {
                // El Admin sí necesita ver TODO, incluso lo borrado lógicamente
                query = query.IgnoreQueryFilters();
            }

            return await query
                .OrderByDescending(m => m.UltimaActualizacion ?? m.FechaCreacion)
                .ToListAsync();
        }

        public async Task GuardarManualAsync(Manual manual, Guid usuarioEditorId)
        {
            using var context = _dbFactory.CreateDbContext();
            TipoAccionManual accion;
            string detalle;

            try
            {
                if (manual.Id == 0)
                {
                    manual.FechaCreacion = DateTime.UtcNow;
                    manual.AutorId = usuarioEditorId;
                    context.Manuales.Add(manual);
                    accion = TipoAccionManual.Creacion;
                    detalle = $"Creó el manual inicial";
                }
                else
                {
                    var manualDb = await context.Manuales.FindAsync(manual.Id);
                    if (manualDb == null) return;

                    manualDb.Titulo = manual.Titulo;
                    manualDb.ContenidoHTML = manual.ContenidoHTML;
                    manualDb.Etiquetas = manual.Etiquetas;
                    manualDb.RolesVisibles = manual.RolesVisibles;
                    manualDb.IsActive = manual.IsActive;
                    manualDb.UltimaActualizacion = DateTime.UtcNow;

                    accion = TipoAccionManual.Edicion;
                    detalle = "Actualización de contenido/metadatos";
                }

                await context.SaveChangesAsync();

                // Registrar auditoría
                var log = new ManualLog
                {
                    ManualId = manual.Id,
                    UsuarioId = usuarioEditorId,
                    Accion = accion,
                    Detalle = detalle
                };
                context.ManualLogs.Add(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar manual {Id}", manual.Id);
                throw; // El componente UI manejará el mensaje amigable
            }
        }

        public async Task EliminarManualAsync(int id, Guid usuarioId, bool esAdmin)
        {
            using var context = _dbFactory.CreateDbContext();
            // Usamos IgnoreQueryFilters para encontrarlo aunque ya esté marcado como borrado
            var manual = await context.Manuales.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == id);
            if (manual == null) return;

            if (esAdmin)
            {
                // Limpieza en cascada manual de logs
                var logs = context.ManualLogs.Where(l => l.ManualId == id);
                context.ManualLogs.RemoveRange(logs);
                context.Manuales.Remove(manual);
                _logger.LogWarning("Admin {User} eliminó físicamente el manual {Id}", usuarioId, id);
            }
            else
            {
                manual.IsDeleted = true;
                manual.UltimaActualizacion = DateTime.UtcNow;

                context.ManualLogs.Add(new ManualLog
                {
                    ManualId = id,
                    UsuarioId = usuarioId,
                    Accion = TipoAccionManual.EliminacionLogica,
                    Detalle = "Movido a papelera"
                });
            }

            await context.SaveChangesAsync();
        }

        public async Task<List<ManualLog>> ObtenerHistorialAsync(int manualId)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.ManualLogs
                .Include(l => l.Usuario)
                .Where(l => l.ManualId == manualId)
                .OrderByDescending(l => l.FechaEvento)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Manual?> ObtenerPorIdAsync(int id)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Manuales
                .Include(m => m.Autor)
                .IgnoreQueryFilters() // Permite al editor ver manuales borrados/inactivos
                .FirstOrDefaultAsync(m => m.Id == id);
        }
    }
}