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
        private readonly ILogger<ManualService> _logger;

        public ManualService(IDbContextFactory<AppDbContext> dbFactory, ILogger<ManualService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        // --- MÉTODOS DE LECTURA ---

        public async Task<List<Manual>> ObtenerTodosAsync(string? rolUsuario = null, string? terminoBusqueda = null)
        {
            using var context = _dbFactory.CreateDbContext();

            // Iniciamos la consulta con todas las relaciones necesarias para la interfaz
            var query = context.Manuales
                .Include(m => m.Autor)
                .Include(m => m.ManualEtiquetas)
                .Include(m => m.RolesVisibles)
                .AsNoTracking();

            // LÓGICA DE SEGURIDAD: Los administradores ven todo (incluyendo eliminados y ocultos)
            if (rolUsuario == RolUsuario.Administrador.ToString())
            {
                query = query.IgnoreQueryFilters();
            }
            else
            {
                // Usuarios y Asesores solo ven manuales ACTIVOS que coincidan con su rol o sean públicos
                query = query.Where(m => m.IsActive &&
                    (!m.RolesVisibles.Any() ||
                     (!string.IsNullOrEmpty(rolUsuario) && m.RolesVisibles.Any(r => r.RolNombre == rolUsuario))));
            }

            // BÚSQUEDA ROBUSTA: Título o Etiquetas
            if (!string.IsNullOrWhiteSpace(terminoBusqueda))
            {
                terminoBusqueda = terminoBusqueda.Trim().ToLower();
                query = query.Where(m => m.Titulo.ToLower().Contains(terminoBusqueda) ||
                                         m.ManualEtiquetas.Any(e => e.Etiqueta.ToLower().Contains(terminoBusqueda)));
            }

            return await query
                .OrderByDescending(m => m.UltimaActualizacion ?? m.FechaCreacion)
                .ToListAsync();
        }

        public async Task<Manual?> ObtenerPorIdAsync(int id)
        {
            using var context = _dbFactory.CreateDbContext();
            // Usamos IgnoreQueryFilters para que el editor pueda cargar manuales "eliminados" (Soft Delete)
            return await context.Manuales
                .Include(m => m.Autor)
                .Include(m => m.ManualEtiquetas)
                .Include(m => m.RolesVisibles)
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        // --- MÉTODOS DE ESCRITURA ---

        public async Task GuardarManualAsync(Manual manual, Guid usuarioEditorId)
        {
            using var context = _dbFactory.CreateDbContext();

            // Validación estricta de permisos en la capa de servicio
            await ValidarAccesoAsync(context, usuarioEditorId);

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
                    // CARGA PARA ACTUALIZACIÓN: Cargamos las colecciones para sincronizarlas
                    var manualDb = await context.Manuales
                        .Include(m => m.ManualEtiquetas)
                        .Include(m => m.RolesVisibles)
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(m => m.Id == manual.Id);

                    if (manualDb == null) return;

                    // Actualización de campos básicos
                    manualDb.Titulo = manual.Titulo;
                    manualDb.ContenidoHTML = manual.ContenidoHTML;
                    manualDb.IsActive = manual.IsActive;
                    manualDb.UltimaActualizacion = DateTime.UtcNow;

                    // CORRECCIÓN: Sincronización manual de Etiquetas (Evita duplicados)
                    context.ManualEtiquetas.RemoveRange(manualDb.ManualEtiquetas);
                    foreach (var et in manual.ManualEtiquetas)
                    {
                        manualDb.ManualEtiquetas.Add(new ManualEtiqueta { Etiqueta = et.Etiqueta });
                    }

                    // CORRECCIÓN: Sincronización manual de Roles de Visibilidad
                    context.ManualRolesVisibilidad.RemoveRange(manualDb.RolesVisibles);
                    foreach (var rv in manual.RolesVisibles)
                    {
                        manualDb.RolesVisibles.Add(new ManualRolVisibilidad { RolNombre = rv.RolNombre });
                    }
                }

                await context.SaveChangesAsync();

                // Registro de Auditoría
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
                _logger.LogError(ex, "Error al guardar el manual {Id}", manual.Id);
                throw;
            }
        }

        public async Task EliminarManualAsync(int id, Guid usuarioId, bool esAdmin)
        {
            using var context = _dbFactory.CreateDbContext();
            var manual = await context.Manuales.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == id);

            if (manual == null) return;

            if (esAdmin)
            {
                // Hard Delete para administradores
                context.Manuales.Remove(manual);
            }
            else
            {
                // Soft Delete para el resto de usuarios autorizados
                manual.IsDeleted = true;
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

        private async Task ValidarAccesoAsync(AppDbContext context, Guid userId)
        {
            if (userId == Guid.Empty) throw new UnauthorizedAccessException("Sesión no válida.");

            var usuario = await context.Usuarios
                .AsNoTracking()
                .Select(u => new { u.Id, u.Rol, u.IsActive })
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null || !usuario.IsActive ||
               (usuario.Rol != RolUsuario.Administrador && usuario.Rol != RolUsuario.Asesor))
            {
                throw new UnauthorizedAccessException("No tiene permisos para gestionar manuales.");
            }
        }
    }
}