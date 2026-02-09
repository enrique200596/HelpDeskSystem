using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public AppDbContext() { }

        // Entidades Generales
        public DbSet<Ticket> Tickets { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Mensaje> Mensajes { get; set; } = null!;
        public DbSet<Categoria> Categorias { get; set; } = null!;

        // --- MÓDULO DE MANUALES (CORRECCIÓN 100/100) ---
        public DbSet<Manual> Manuales { get; set; } = null!;
        public DbSet<ManualLog> ManualLogs { get; set; } = null!;
        public DbSet<ManualEtiqueta> ManualEtiquetas { get; set; } = null!;
        public DbSet<ManualRolVisibilidad> ManualRolesVisibilidad { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Configuración de respaldo para herramientas de diseño (Migrations)
                optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HelpDeskUtepsa;Trusted_Connection=True;MultipleActiveResultSets=true");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================================
            // 1. CONFIGURACIÓN DEL MÓDULO DE MANUALES (RBAC e INTEGRIDAD)
            // ============================================================

            // Relación Manual -> Etiquetas (Borrado en Cascada Físico)
            modelBuilder.Entity<ManualEtiqueta>()
                .HasOne(e => e.Manual)
                .WithMany(m => m.ManualEtiquetas)
                .HasForeignKey(e => e.ManualId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación Manual -> Roles de Visibilidad (Borrado en Cascada Físico)
            modelBuilder.Entity<ManualRolVisibilidad>()
                .HasOne(rv => rv.Manual)
                .WithMany(m => m.RolesVisibles)
                .HasForeignKey(rv => rv.ManualId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación Manual -> Historial (Restrict: Protegemos la auditoría)
            modelBuilder.Entity<ManualLog>()
                .HasOne(log => log.Manual)
                .WithMany()
                .HasForeignKey(log => log.ManualId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación ManualLog -> Usuario (Auditoría)
            modelBuilder.Entity<ManualLog>()
                .HasOne(log => log.Usuario)
                .WithMany()
                .HasForeignKey(log => log.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================================================
            // 2. FILTROS GLOBALES DE SEGURIDAD (Manejo de Papelera)
            // ============================================================

            // Filtro para Manuales (Soft Delete)
            modelBuilder.Entity<Manual>().HasQueryFilter(m => !m.IsDeleted);

            // MEJORA 100/100: Filtros dependientes para integridad de consultas.
            // Si un manual está en la papelera, sus etiquetas y roles no deben aparecer en búsquedas globales.
            modelBuilder.Entity<ManualEtiqueta>()
                .HasQueryFilter(e => !e.Manual!.IsDeleted);

            modelBuilder.Entity<ManualRolVisibilidad>()
                .HasQueryFilter(rv => !rv.Manual!.IsDeleted);

            // Filtros de otras entidades
            modelBuilder.Entity<Ticket>().HasQueryFilter(t => !t.IsDeleted);
            modelBuilder.Entity<Usuario>().HasQueryFilter(u => u.IsActive);
            modelBuilder.Entity<Categoria>().HasQueryFilter(c => c.IsActive);
            modelBuilder.Entity<Mensaje>().HasQueryFilter(m => !m.Ticket.IsDeleted);

            // ============================================================
            // 3. CONVERSIÓN GLOBAL A UTC (ESTANDARIZACIÓN)
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

            // ============================================================
            // 4. SEMILLA DE DATOS (SEED DATA)
            // ============================================================
            modelBuilder.Entity<Categoria>().HasData(
                new Categoria { Id = 1, Nombre = "Sistema Financiero", IsActive = true },
                new Categoria { Id = 2, Nombre = "Sistema Genesis", IsActive = true },
                new Categoria { Id = 3, Nombre = "Reportes", IsActive = true }
            );
        }
    }
}