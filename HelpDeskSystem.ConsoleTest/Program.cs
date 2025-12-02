using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;

Console.WriteLine("--- Iniciando Base de Datos ---");

using (var context = new AppDbContext())
{
    // Crear un ticket nuevo
    var nuevoTicket = new Ticket
    {
        Titulo = "PC no enciende",
        Descripcion = "Huele a quemado",
        UsuarioId = Guid.NewGuid(),
        EsUrgente = true,
        Estado = EstadoTicket.Abierto
    };

    context.Tickets.Add(nuevoTicket);
    context.SaveChanges(); // <--- Aquí ocurre el INSERT SQL

    Console.WriteLine($"¡Éxito! Ticket guardado con ID: {nuevoTicket.Id}");
}