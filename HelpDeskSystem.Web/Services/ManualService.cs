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

        public async Task<List<Manual>> ObtenerTodosAsync(string? rolUsuario = null, string? terminoBusqueda = null)
        {
            using var context = _dbFactory.CreateDbContext();

            // 1. Configuración base: Incluimos las relaciones normalizadas (Autor, Etiquetas, Roles)
            var query = context.Manuales
                .Include(m => m.Autor)
                .Include(m => m.ManualEtiquetas)
                .Include(m => m.RolesVisibles)
                .AsNoTracking()
                .AsQueryable();

            // 2. SEGURIDAD: Filtro de Visibilidad por Rol (Lógica Normalizada)
            if (rolUsuario != nameof(RolUsuario.Administrador))
            {
                // Usuarios normales: Solo activos Y (públicos O coincidentes con su rol)
                query = query.Where(m => m.IsActive &&
                    (!m.RolesVisibles.Any() || // Si la lista de roles está vacía, es público
                     (!string.IsNullOrEmpty(rolUsuario) && m.RolesVisibles.Any(r => r.RolNombre == rolUsuario))));
            }
            else
            {
                // Administradores: Ven todo (incluido borrados y borradores)
                query = query.IgnoreQueryFilters();
            }

            // 3. BÚSQUEDA: Filtro SQL optimizado sobre colecciones
            if (!string.IsNullOrEmpty(terminoBusqueda))
            {
                query = query.Where(m => m.Titulo.Contains(terminoBusqueda) ||
                                         m.ManualEtiquetas.Any(e => e.Etiqueta.Contains(terminoBusqueda)));
            }

            // 4. Ordenar por fecha más reciente
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
            // --- CORRECCIÓN DE SEGURIDAD: Validación real ---
            ValidarIdentidad(usuarioEditorId, "Guardar/Editar Manual");

            using var context = _dbFactory.CreateDbContext();
            TipoAccionManual accion;
            string detalle;

            try
            {
                if (manual.Id == 0)
                {
                    // --- CREACIÓN ---
                    manual.FechaCreacion = DateTime.UtcNow;
                    manual.AutorId = usuarioEditorId;

                    // EF Core insertará automáticamente los hijos en ManualEtiquetas y RolesVisibles
                    context.Manuales.Add(manual);

                    accion = TipoAccionManual.Creacion;
                    detalle = $"Creó el manual inicial";
                }
                else
                {
                    // --- EDICIÓN (Manejo de Relaciones) ---
                    var manualDb = await context.Manuales
                        .Include(m => m.ManualEtiquetas)
                        .Include(m => m.RolesVisibles)
                        .FirstOrDefaultAsync(m => m.Id == manual.Id);

                    if (manualDb == null) return;

                    // Actualizar campos escalares
                    manualDb.Titulo = manual.Titulo;
                    manualDb.ContenidoHTML = manual.ContenidoHTML;
                    manualDb.IsActive = manual.IsActive;
                    manualDb.UltimaActualizacion = DateTime.UtcNow;

                    // Actualizar Colección Etiquetas (Estrategia: Limpiar y Reemplazar)
                    manualDb.ManualEtiquetas.Clear();
                    foreach (var tag in manual.ManualEtiquetas)
                    {
                        manualDb.ManualEtiquetas.Add(new ManualEtiqueta { Etiqueta = tag.Etiqueta });
                    }

                    // Actualizar Colección Roles
                    manualDb.RolesVisibles.Clear();
                    foreach (var rol in manual.RolesVisibles)
                    {
                        manualDb.RolesVisibles.Add(new ManualRolVisibilidad { RolNombre = rol.RolNombre });
                    }

                    accion = TipoAccionManual.Edicion;
                    detalle = "Actualización de contenido/metadatos";
                }

                await context.SaveChangesAsync();

                // Registro de Auditoría
                var log = new ManualLog
                {
                    ManualId = manual.Id,
                    UsuarioId = usuarioEditorId,
                    Accion = accion,
                    Detalle = detalle,
                    FechaEvento = DateTime.UtcNow
                };
                context.ManualLogs.Add(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar manual {Id}", manual.Id);
                throw;
            }
        }

        public async Task EliminarManualAsync(int id, Guid usuarioId, bool esAdmin)
        {
            ValidarIdentidad(usuarioId, "Eliminar Manual");

            using var context = _dbFactory.CreateDbContext();
            var manual = await context.Manuales.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == id);
            if (manual == null) return;

            if (esAdmin)
            {
                // Borrado Físico: EF Core maneja el borrado en cascada de etiquetas/logs si está configurado en DB,
                // pero por seguridad limpiamos logs explícitamente.
                var logs = context.ManualLogs.Where(l => l.ManualId == id);
                context.ManualLogs.RemoveRange(logs);

                // Las etiquetas/roles se borran por cascada (FK) o se pueden borrar explícitamente aquí si fuera necesario.
                context.Manuales.Remove(manual);

                _logger.LogWarning("Admin {User} eliminó físicamente el manual {Id}", usuarioId, id);
            }
            else
            {
                // Borrado Lógico (Soft Delete)
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

        private void ValidarIdentidad(Guid userId, string operacion)
        {
            // --- CORRECCIÓN: Eliminado el ID Fantasma Hardcodeado ---
            if (userId == Guid.Empty)
            {
                var error = $"Intento de operación no autorizada ({operacion}) con identidad anónima/vacía.";
                _logger.LogCritical(error);
                throw new UnauthorizedAccessException("Error de Seguridad: Sesión no válida.");
            }
        }
    }
}