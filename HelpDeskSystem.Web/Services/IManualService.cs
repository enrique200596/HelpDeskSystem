using HelpDeskSystem.Domain.Entities;

namespace HelpDeskSystem.Web.Services
{
    public interface IManualService
    {
        Task<List<Manual>> ObtenerTodosAsync();
        Task<Manual?> ObtenerPorIdAsync(int id);
        Task GuardarManualAsync(Manual manual);
        Task EliminarManualAsync(int id);
    }
}