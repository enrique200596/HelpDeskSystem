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

        // 2. Inicializar Categorías (Esencial para crear tickets)
        if (!await context.Categorias.AnyAsync())
        {
            var categorias = new List<Categoria>
            {
                new Categoria { Nombre = "Software", IsActive = true },
                new Categoria { Nombre = "Hardware", IsActive = true },
                new Categoria { Nombre = "Redes", IsActive = true },
                new Categoria { Nombre = "Cuentas y Accesos", IsActive = true }
            };
            context.Categorias.AddRange(categorias);
            await context.SaveChangesAsync();
        }

        // 3. Obtener credenciales de appsettings
        var adminEmail = configuration["InitialSetup:AdminEmail"] ?? "admin@helpdesk.com";
        var adminPassword = configuration["InitialSetup:AdminPassword"];

        if (string.IsNullOrEmpty(adminPassword)) return;

        // 4. Verificar si el administrador ya existe
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
                IsActive = true,
                // CORRECCIÓN: Evita error de NOT NULL en base de datos
                FotoPerfilUrl = "/img/default-avatar.png"
            };

            context.Usuarios.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
