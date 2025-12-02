using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class TicketService
    {
        private readonly AppDbContext _context;

        public TicketService(AppDbContext context)
        {
            _context = context;
        }

        // --- LECTURA ---

        // 1. Método para obtener las categorías (para el dropdown de Nuevo Ticket)
        public async Task<List<Categoria>> ObtenerCategoriasAsync()
        {
            return await _context.Categorias.ToListAsync();
        }

        // 2. Método de Bandeja Inteligente (Tu lógica estaba bien aquí)
        public async Task<List<Ticket>> ObtenerTicketsFiltradosAsync(Guid userId, string rol)
        {
            var query = _context.Tickets
                .Include(t => t.Usuario)
                .Include(t => t.Asesor)
                .Include(t => t.Categoria) // Importante para ver la categoría en la lista
                .AsQueryable();

            if (rol == "Administrador")
            {
                // Ve todo
            }
            else if (rol == "Asesor")
            {
                // LÓGICA DE FILTRADO POR CATEGORÍA
                var idsCategoriasDelAsesor = await _context.Usuarios
                    .Where(u => u.Id == userId)
                    .SelectMany(u => u.Categorias.Select(c => c.Id))
                    .ToListAsync();

                // Muestra si: (Es mío) O (Está libre Y es de mi categoría)
                query = query.Where(t =>
                    t.AsesorId == userId ||
                    (t.AsesorId == null && idsCategoriasDelAsesor.Contains(t.CategoriaId))
                );
            }
            else if (rol == "Usuario")
            {
                query = query.Where(t => t.UsuarioId == userId);
            }
            else
            {
                return new List<Ticket>();
            }

            return await query
                .OrderByDescending(t => t.EsUrgente)
                .ThenByDescending(t => t.FechaCreacion)
                .ToListAsync();
        }

        // 3. Método para ver un solo ticket (AQUÍ FALTABA EL INCLUDE)
        public async Task<Ticket?> ObtenerPorIdAsync(int id)
        {
            return await _context.Tickets
                .Include(t => t.Usuario)
                .Include(t => t.Asesor)
                .Include(t => t.Categoria) // <--- ESTO FALTABA EN TU CÓDIGO
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        }

        // --- ESCRITURA ---

        public async Task GuardarTicketAsync(Ticket ticket)
        {
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
        }

        public async Task ActualizarDescripcionUsuarioAsync(Ticket ticketModificado)
        {
            var ticketDb = await _context.Tickets.FindAsync(ticketModificado.Id);
            if (ticketDb != null)
            {
                if (ticketDb.FueEditado)
                    throw new InvalidOperationException("Este ticket ya fue editado una vez.");

                ticketDb.Titulo = ticketModificado.Titulo;
                ticketDb.Descripcion = ticketModificado.Descripcion;
                ticketDb.FueEditado = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task AsignarTicketAsync(int ticketId, Guid asesorId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket != null)
            {
                ticket.AsesorId = asesorId;
                ticket.Estado = EstadoTicket.Asignado;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ResolverTicketAsync(int id, Guid usuarioEjecutorId)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null || ticket.IsDeleted) return;

            // Validación de asesor (ya la tenías)
            if (ticket.AsesorId != usuarioEjecutorId)
            {
                throw new UnauthorizedAccessException("Solo el asesor asignado puede resolver.");
            }

            if (ticket.Estado != EstadoTicket.Resuelto)
            {
                ticket.Estado = EstadoTicket.Resuelto;

                // NUEVA LÍNEA: Guardamos la estampa de tiempo final
                ticket.FechaCierre = DateTime.Now;

                await _context.SaveChangesAsync();
            }
        }

        public async Task CalificarTicketAsync(int ticketId, int estrellas, Guid usuarioCalificadorId)
        {
            if (estrellas < 1 || estrellas > 5) throw new ArgumentOutOfRangeException("1-5");

            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket != null && ticket.UsuarioId == usuarioCalificadorId && ticket.Estado == EstadoTicket.Resuelto)
            {
                ticket.SatisfaccionUsuario = estrellas;
                await _context.SaveChangesAsync();
            }
        }
    }
}