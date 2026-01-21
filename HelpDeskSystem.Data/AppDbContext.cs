using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HelpDeskSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public AppDbContext() { }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Mensaje> Mensajes { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Manual> Manuales { get; set; }
        public DbSet<ManualLog> ManualLogs { get; set; }

        // ADICIÓN NECESARIA: Configuración para entornos sin Inyección de Dependencias
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Esta cadena debe coincidir con la de tu appsettings.json
                optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HelpDeskUtepsa;Trusted_Connection=True;MultipleActiveResultSets=true");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================================
            // 1. CONFIGURACIÓN DE RELACIONES Y COMPORTAMIENTO DE BORRADO
            // ============================================================

            // Ticket -> Usuario (Restrict: No borrar usuario si tiene tickets)
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Usuario)
                .WithMany()
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // ManualLog -> Usuario (Restrict: No borrar usuario si tiene historial)
            modelBuilder.Entity<ManualLog>()
                .HasOne(log => log.Usuario)
                .WithMany()
                .HasForeignKey(log => log.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================================================
            // 2. RELACIÓN MUCHOS A MUCHOS (ASESORES - CATEGORÍAS)
            // ============================================================
            modelBuilder.Entity<Usuario>()
                .HasMany(u => u.Categorias)
                .WithMany(c => c.Asesores)
                .UsingEntity<Dictionary<string, object>>(
                    "UsuarioCategoria",
                    j => j.HasOne<Categoria>().WithMany().HasForeignKey("CategoriasId"),
                    j => j.HasOne<Usuario>().WithMany().HasForeignKey("AsesoresId")
                );

            // ============================================================
            // 3. SEMILLA DE DATOS (SEED DATA)
            // ============================================================
            modelBuilder.Entity<Categoria>().HasData(
                new Categoria { Id = 1, Nombre = "Sistema Financiero", IsActive = true },
                new Categoria { Id = 2, Nombre = "Sistema Genesis", IsActive = true },
                new Categoria { Id = 3, Nombre = "Reportes", IsActive = true }
            );

            // ============================================================
            // 4. FILTROS GLOBALES (SEGURIDAD E INTEGRIDAD)
            // ============================================================

            // Entidades principales (Soft Delete y Activos)
            modelBuilder.Entity<Ticket>().HasQueryFilter(t => !t.IsDeleted);
            modelBuilder.Entity<Manual>().HasQueryFilter(m => !m.IsDeleted);
            modelBuilder.Entity<Usuario>().HasQueryFilter(u => u.IsActive);
            modelBuilder.Entity<Categoria>().HasQueryFilter(c => c.IsActive);

            // CORRECCIÓN 10/10: Filtros para entidades hijas (Evita advertencias EF 10622)
            // Esto asegura que si un Ticket se "elimina", sus mensajes también se oculten automáticamente
            modelBuilder.Entity<Mensaje>()
                .HasQueryFilter(m => !m.Ticket.IsDeleted);

            // Esto asegura que si un Manual se "elimina", su bitácora de cambios también se oculte
            modelBuilder.Entity<ManualLog>()
                .HasQueryFilter(ml => !ml.Manual.IsDeleted);

            // ============================================================
            // 5. CONVERSIÓN GLOBAL A UTC (ESTANDARIZACIÓN TEMPORAL)
            // ============================================================
            var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                v => v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                v => v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                        property.SetValueConverter(dateTimeConverter);
                    else if (property.ClrType == typeof(DateTime?))
                        property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }
}