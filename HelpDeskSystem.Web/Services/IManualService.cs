using HelpDeskSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HelpDeskSystem.Web.Services
{
    /// <summary>
    /// Define el contrato para la gestión de manuales, asegurando la integridad de datos,
    /// el control de acceso basado en roles (RBAC) y la trazabilidad de operaciones.
    /// </summary>
    public interface IManualService
    {
        /// <summary>
        /// Obtiene una lista de manuales autorizados, permitiendo el filtrado por rol de visibilidad y búsqueda textual.
        /// </summary>
        /// <param name="rolUsuario">Nombre del rol del usuario para filtrar el contenido visible.</param>
        /// <param name="terminoBusqueda">Palabra clave para filtrar por título o etiquetas (opcional).</param>
        /// <returns>Una colección de manuales que cumplen con los criterios de seguridad y búsqueda.</returns>
        Task<List<Manual>> ObtenerTodosAsync(string? rolUsuario = null, string? terminoBusqueda = null);

        /// <summary>
        /// Recupera la información detallada de un manual específico por su identificador.
        /// </summary>
        /// <param name="id">Identificador único del manual.</param>
        /// <returns>La entidad Manual con sus relaciones cargadas, o null si no se encuentra.</returns>
        Task<Manual?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Procesa la persistencia de un manual (Creación o Edición), validando la identidad del autor/editor.
        /// </summary>
        /// <param name="manual">Entidad manual con datos y relaciones actualizadas.</param>
        /// <param name="usuarioEditorId">ID del usuario que ejecuta la acción para fines de auditoría y validación.</param>
        /// <exception cref="UnauthorizedAccessException">Se lanza si el usuario no tiene permisos suficientes o la identidad es inválida.</exception>
        Task GuardarManualAsync(Manual manual, Guid usuarioEditorId);

        /// <summary>
        /// Gestiona la eliminación de un manual, aplicando borrado físico para administradores y lógico para otros roles autorizados.
        /// </summary>
        /// <param name="id">ID del manual a eliminar.</param>
        /// <param name="usuarioId">ID del usuario que solicita la baja.</param>
        /// <param name="esAdmin">Indica si se posee nivel de administrador para proceder con un borrado físico.</param>
        /// <exception cref="UnauthorizedAccessException">Se lanza si se detecta un intento de operación no autorizada.</exception>
        Task EliminarManualAsync(int id, Guid usuarioId, bool esAdmin);

        /// <summary>
        /// Consulta el registro histórico de todas las acciones realizadas sobre un manual específico.
        /// </summary>
        /// <param name="manualId">Identificador del manual consultado.</param>
        /// <returns>Lista de logs de auditoría ordenados cronológicamente de forma descendente.</returns>
        Task<List<ManualLog>> ObtenerHistorialAsync(int manualId);
    }
}