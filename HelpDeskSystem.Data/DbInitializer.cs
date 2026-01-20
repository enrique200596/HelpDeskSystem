using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public static class DbInitializer
{
    public static async Task SeedData(AppDbContext context, IConfiguration configuration)
    {
        // 1. Asegurar que las migraciones estén aplicadas
        await context.Database.MigrateAsync();

        // 2. Obtener credenciales de appsettings
        var adminEmail = configuration["InitialSetup:AdminEmail"] ?? "admin@helpdesk.com";
        var adminPassword = configuration["InitialSetup:AdminPassword"];

        if (string.IsNullOrEmpty(adminPassword)) return; // Evitar crear usuario sin pass

        // 3. Verificar si el administrador ya existe
        var adminExists = await context.Usuarios.AnyAsync(u => u.Email == adminEmail);

        if (!adminExists)
        {
            var admin = new Usuario
            {
                Id = Guid.NewGuid(),
                Nombre = "Administrador Sistema",
                Email = adminEmail,
                Rol = RolUsuario.Administrador,
                Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                IsActive = true
            };

            context.Usuarios.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}