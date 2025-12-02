namespace HelpDeskSystem.Web.Auth
{
    public class UserSession
    {
        // Inicializamos con "string.Empty" para evitar nulos
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}