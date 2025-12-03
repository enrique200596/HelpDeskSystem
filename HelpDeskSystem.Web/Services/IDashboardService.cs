namespace HelpDeskSystem.Web.Services
{
    public interface IDashboardService
    {
        Task<DashboardDto> ObtenerMetricasAsync(Guid usuarioId, string rol);
    }
}
