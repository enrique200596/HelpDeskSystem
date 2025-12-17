using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
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

        public async Task<List<Manual>> ObtenerTodosAsync(string? rolUsuario = null)
        {
            using var context = _dbFactory.CreateDbContext();

            var query = context.Manuales
                .Include(m => m.Autor)
                .AsNoTracking()
                .AsQueryable();

            // 1. Regla de Eliminados
            // Si NO es administrador, solo ve manuales activos y no eliminados
            if (rolUsuario != nameof(RolUsuario.Administrador))
            {
                query = query.Where(m => !m.IsDeleted && m.IsActive);
            }

            // 2. Regla de Visibilidad por Roles (RBAC)
            // Si NO es administrador, verificamos si tiene permiso
            if (rolUsuario != nameof(RolUsuario.Administrador))
            {
                // Visible si:
                // a) RolesVisibles está vacío (es público)
                // b) O si RolesVisibles contiene mi rol (validando que mi rol no sea nulo)
                query = query.Where(m =>
                    string.IsNullOrEmpty(m.RolesVisibles) ||
                    (!string.IsNullOrEmpty(rolUsuario) && m.RolesVisibles.Contains(rolUsuario)));
            }

            // --- IMPORTANTE: Aquí está el retorno que faltaba ---
            return await query
                .OrderByDescending(m => m.UltimaActualizacion ?? m.FechaCreacion)
                .ToListAsync();
        }

        public async Task<Manual?> ObtenerPorIdAsync(int id)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Manuales
                .Include(m => m.Autor)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task GuardarManualAsync(Manual manual, Guid usuarioEditorId)
        {
            using var context = _dbFactory.CreateDbContext();
            string accion = "";
            string detalle = "";

            if (manual.Id == 0)
            {
                // CREACIÓN
                manual.FechaCreacion = DateTime.Now;
                manual.IsDeleted = false;
                manual.IsActive = true;

                // ASIGNAR AUTOR (CRÍTICO PARA QUE NO FALLE SQL)
                manual.AutorId = usuarioEditorId;

                context.Manuales.Add(manual);

                accion = "Creación";
                detalle = $"Creó el manual '{manual.Titulo}'";
            }
            else
            {
                // EDICIÓN
                var manualDb = await context.Manuales.FindAsync(manual.Id);
                if (manualDb != null)
                {
                    manualDb.Titulo = manual.Titulo;
                    manualDb.ContenidoHTML = manual.ContenidoHTML;
                    manualDb.Etiquetas = manual.Etiquetas;
                    manualDb.RolesVisibles = manual.RolesVisibles;
                    manualDb.IsActive = manual.IsActive;
                    manualDb.UltimaActualizacion = DateTime.Now;

                    // Nota: No cambiamos el Autor original al editar

                    accion = "Edición";
                    detalle = $"Actualizó el contenido o propiedades.";
                }
            }

            await context.SaveChangesAsync();

            // REGISTRAR HISTORIAL (LOG)
            if (manual.Id > 0)
            {
                var log = new ManualLog
                {
                    ManualId = manual.Id,
                    UsuarioId = usuarioEditorId,
                    Accion = accion,
                    Detalle = detalle,
                    FechaEvento = DateTime.Now
                };
                context.ManualLogs.Add(log);
                await context.SaveChangesAsync();
            }
        }

        public async Task EliminarManualAsync(int id, Guid usuarioId, bool esAdmin)
        {
            using var context = _dbFactory.CreateDbContext();
            var manual = await context.Manuales.FindAsync(id);

            if (manual == null) return;

            if (esAdmin)
            {
                // ELIMINACIÓN DEFINITIVA (Solo Admin)
                // Borramos logs manualmente antes para evitar conflictos si la cascada falla
                var logs = context.ManualLogs.Where(l => l.ManualId == id);
                context.ManualLogs.RemoveRange(logs);

                context.Manuales.Remove(manual);
            }
            else
            {
                // ELIMINACIÓN LÓGICA (Asesores)
                manual.IsDeleted = true;
                manual.UltimaActualizacion = DateTime.Now;

                var log = new ManualLog
                {
                    ManualId = id,
                    UsuarioId = usuarioId,
                    Accion = "Eliminación Lógica",
                    Detalle = "El usuario movió el manual a la papelera (oculto).",
                    FechaEvento = DateTime.Now
                };
                context.ManualLogs.Add(log);
            }

            await context.SaveChangesAsync();
        }

        public async Task<List<ManualLog>> ObtenerHistorialAsync(int manualId)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.ManualLogs
                .Include(l => l.Usuario) // <--- ESTO ES VITAL PARA QUE SALGA EL NOMBRE
                .Where(l => l.ManualId == manualId)
                .OrderByDescending(l => l.FechaEvento)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}