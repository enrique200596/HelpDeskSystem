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

            // MEJORA 100/100: Validación de existencia y permisos de rol en BD
            await ValidarAccesoAsync(context, usuarioEditorId, "Guardar/Editar Manual");

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
                    detalle = "Creó el manual inicial";
                }
                else
                {
                    var manualDb = await context.Manuales
                        .Include(m => m.ManualEtiquetas)
                        .Include(m => m.RolesVisibles)
                        .FirstOrDefaultAsync(m => m.Id == manual.Id);

                    if (manualDb == null) return;

                    manualDb.Titulo = manual.Titulo;
                    manualDb.ContenidoHTML = manual.ContenidoHTML;
                    manualDb.IsActive = manual.IsActive;
                    manualDb.UltimaActualizacion = DateTime.UtcNow;

                    manualDb.ManualEtiquetas.Clear();
                    foreach (var tag in manual.ManualEtiquetas)
                    {
                        manualDb.ManualEtiquetas.Add(new ManualEtiqueta { Etiqueta = tag.Etiqueta });
                    }

                    manualDb.RolesVisibles.Clear();
                    foreach (var rol in manual.RolesVisibles)
                    {
                        manualDb.RolesVisibles.Add(new ManualRolVisibilidad { RolNombre = rol.RolNombre });
                    }

                    accion = TipoAccionManual.Edicion;
                    detalle = "Actualización de contenido/metadatos";
                }

                await context.SaveChangesAsync();

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
            using var context = _dbFactory.CreateDbContext();

            // MEJORA 100/100: Validación de integridad de identidad antes de eliminar
            await ValidarAccesoAsync(context, usuarioId, "Eliminar Manual");

            var manual = await context.Manuales.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == id);
            if (manual == null) return;

            if (esAdmin)
            {
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

        /// <summary>
        /// Validación de seguridad robusta: Comprueba que el ID no sea nulo, 
        /// que el usuario exista en la BD y que posea un rol autorizado para gestionar manuales.
        /// </summary>
        private async Task ValidarAccesoAsync(AppDbContext context, Guid userId, string operacion)
        {
            if (userId == Guid.Empty)
            {
                _logger.LogCritical("Intento de operación {Operacion} sin GUID de usuario.", operacion);
                throw new UnauthorizedAccessException("Sesión no válida.");
            }

            var usuario = await context.Usuarios
                .AsNoTracking()
                .Select(u => new { u.Id, u.Rol })
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null)
            {
                _logger.LogCritical("Identidad falsificada detectada. Usuario {UserId} no existe en BD para {Operacion}", userId, operacion);
                throw new UnauthorizedAccessException("El usuario no tiene una cuenta válida en el sistema.");
            }

            // Validación de rol: Solo Administradores y Asesores pueden realizar cambios en manuales
            if (usuario.Rol != RolUsuario.Administrador && usuario.Rol != RolUsuario.Asesor)
            {
                _logger.LogWarning("Acceso denegado: El usuario {UserId} (Rol: {Rol}) intentó {Operacion}", userId, usuario.Rol, operacion);
                throw new UnauthorizedAccessException("No tiene permisos suficientes para gestionar manuales.");
            }
        }
    }
}