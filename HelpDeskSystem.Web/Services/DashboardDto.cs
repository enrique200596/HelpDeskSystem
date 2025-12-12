namespace HelpDeskSystem.Web.Services
{
    public class DashboardDto
    {
        public int TotalTickets { get; set; }
        public int Resueltos { get; set; }
        public int Pendientes { get; set; }
        public double PromedioSatisfaccion { get; set; }
    }
}