using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace HelpDeskSystem.Web.Services
{
    public class ManualService : IManualService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<ManualService> _logger;

        public ManualService(IDbContextFactory<AppDbContext> dbFactory, ILogger<ManualService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<List<Manual>> ObtenerTodosAsync(string? rolUsuario = null, string? terminoBusqueda = null)
        {
            using var context = _dbFactory.CreateDbContext();
            var query = context.Manuales
                .Include(m => m.Autor)
                .Include(m => m.ManualEtiquetas)
                .Include(m => m.RolesVisibles)
                .AsNoTracking()
                .AsQueryable();

            if (rolUsuario != nameof(RolUsuario.Administrador))
            {
                query = query.Where(m => m.IsActive &&
                    (!m.RolesVisibles.Any() ||
                     (!string.IsNullOrEmpty(rolUsuario) && m.RolesVisibles.Any(r => r.RolNombre == rolUsuario))));
            }
            else
            {
                query = query.IgnoreQueryFilters();
            }

            if (!string.IsNullOrEmpty(terminoBusqueda))
            {
                query = query.Where(m => m.Titulo.Contains(terminoBusqueda) ||
                                         m.ManualEtiquetas.Any(e => e.Etiqueta.Contains(terminoBusqueda)));
            }

            return await query
                .OrderByDescending(m => m.UltimaActualizacion ?? m.FechaCreacion)
                .ToListAsync();
        }

        public async Task<Manual?> ObtenerPorIdAsync(int id)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Manuales
                .Include(m => m.Autor)
                .Include(m => m.ManualEtiquetas)
                .Include(m => m.RolesVisibles)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task GuardarManualAsync(Manual manual, Guid usuarioEditorId)
        {
            using var context = _dbFactory.CreateDbContext();
            await ValidarAccesoAsync(context, usuarioEditorId, "Guardar Manual");

            try
            {
                if (manual.Id == 0)
                {
                    manual.FechaCreacion = DateTime.UtcNow;
                    manual.AutorId = usuarioEditorId;
                    context.Manuales.Add(manual);
                }
                else
                {
                    var manualDb = await context.Manuales
                        .Include(m => m.ManualEtiquetas)
                        .Include(m => m.RolesVisibles)
                        .FirstOrDefaultAsync(m => m.Id == manual.Id);

                    if (manualDb == null) return;

                    manualDb.ActualizarContenido(manual.Titulo, manual.ContenidoHTML);
                    manualDb.SetEstado(manual.IsActive);
                    manualDb.AsignarEtiquetas(manual.ManualEtiquetas.Select(e => e.Etiqueta));
                    manualDb.AsignarRolesVisibilidad(manual.RolesVisibles.Select(r => r.RolNombre));
                }

                await context.SaveChangesAsync();

                context.ManualLogs.Add(new ManualLog
                {
                    ManualId = manual.Id,
                    UsuarioId = usuarioEditorId,
                    Accion = manual.Id == 0 ? TipoAccionManual.Creacion : TipoAccionManual.Edicion,
                    FechaEvento = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar manual");
                throw;
            }
        }

        public async Task EliminarManualAsync(int id, Guid usuarioId, bool esAdmin)
        {
            using var context = _dbFactory.CreateDbContext();
            var manual = await context.Manuales.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == id);
            if (manual == null) return;

            if (esAdmin) context.Manuales.Remove(manual);
            else manual.IsDeleted = true;

            await context.SaveChangesAsync();
        }

        public async Task<List<ManualLog>> ObtenerHistorialAsync(int manualId)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.ManualLogs
                .Include(l => l.Usuario)
                .Where(l => l.ManualId == manualId)
                .OrderByDescending(l => l.FechaEvento)
                .AsNoTracking().ToListAsync();
        }

        private async Task ValidarAccesoAsync(AppDbContext context, Guid userId, string operacion)
        {
            if (userId == Guid.Empty) throw new UnauthorizedAccessException("Sesión no válida.");
            var usuario = await context.Usuarios.AsNoTracking().Select(u => new { u.Id, u.Rol }).FirstOrDefaultAsync(u => u.Id == userId);
            if (usuario == null || (usuario.Rol != RolUsuario.Administrador && usuario.Rol != RolUsuario.Asesor))
                throw new UnauthorizedAccessException("Permisos insuficientes.");
        }
    }
}