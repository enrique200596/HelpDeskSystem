using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("--- Iniciando Base de Datos ---");

// CONFIGURACIÓN: Creamos las opciones manualmente para el entorno de Consola
var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer("Server=srv-sistem-saf;Database=HelpDeskDB;User Id=sa;Password=$Sin123$5;TrustServerCertificate=True;");

using (var context = new AppDbContext(optionsBuilder.Options))
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