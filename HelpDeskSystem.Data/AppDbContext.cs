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

            // 1. Configuración de Relaciones
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Usuario)
                .WithMany()
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ManualLog>()
                .HasOne(log => log.Usuario)
                .WithMany()
                .HasForeignKey(log => log.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Relación Muchos a Muchos
            modelBuilder.Entity<Usuario>()
                .HasMany(u => u.Categorias)
                .WithMany(c => c.Asesores)
                .UsingEntity<Dictionary<string, object>>(
                    "UsuarioCategoria",
                    j => j.HasOne<Categoria>().WithMany().HasForeignKey("CategoriasId"),
                    j => j.HasOne<Usuario>().WithMany().HasForeignKey("AsesoresId")
                );

            // 3. Semilla de Categorías
            modelBuilder.Entity<Categoria>().HasData(
                new Categoria { Id = 1, Nombre = "Sistema Financiero" },
                new Categoria { Id = 2, Nombre = "Sistema Genesis" },
                new Categoria { Id = 3, Nombre = "Reportes" }
            );

            // 4. Filtros Globales
            modelBuilder.Entity<Ticket>().HasQueryFilter(t => !t.IsDeleted);
            modelBuilder.Entity<Manual>().HasQueryFilter(m => !m.IsDeleted);
            modelBuilder.Entity<Usuario>().HasQueryFilter(u => u.IsActive);
            modelBuilder.Entity<Categoria>().HasQueryFilter(c => c.IsActive);

            // 5. Conversión Global a UTC
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
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