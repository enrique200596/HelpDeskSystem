using HelpDeskSystem.Domain.Entities;

namespace HelpDeskSystem.Web.Services
{
    public interface IAuthService
    {
        Task<Usuario?> LoginAsync(string email, string password);
    }
}