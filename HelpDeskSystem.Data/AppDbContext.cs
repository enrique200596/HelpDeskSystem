using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Ticket> Tickets { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Mensaje> Mensajes { get; set; } = null!;
        public DbSet<Categoria> Categorias { get; set; } = null!;
        public DbSet<Manual> Manuales { get; set; } = null!;
        public DbSet<ManualLog> ManualLogs { get; set; } = null!;
        public DbSet<ManualEtiqueta> ManualEtiquetas { get; set; } = null!;
        public DbSet<ManualRolVisibilidad> ManualRolesVisibilidad { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ================= :   ===========================================
            // 1. RESOLUCIÓN DE CONFLICTOS DE CASCADA (ERROR 1785)
            // ============================================================

            // Mensaje -> Usuario
            modelBuilder.Entity<Mensaje>()
                .HasOne(m => m.Usuario)
                .WithMany()
                .HasForeignKey(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ticket -> Usuario y Asesor
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Usuario)
                .WithMany()
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Asesor)
                .WithMany()
                .HasForeignKey(t => t.AsesorId)
                .OnDelete(DeleteBehavior.Restrict);

            // CORRECCIÓN CRÍTICA: ManualLog -> Usuario (Evita el ciclo de cascada)
            modelBuilder.Entity<ManualLog>()
                .HasOne(log => log.Usuario)
                .WithMany()
                .HasForeignKey(log => log.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // ================= :   ===========================================
            // 2. CONFIGURACIÓN DE FILTROS GLOBALES (SOFT DELETE)
            // ============================================================
            modelBuilder.Entity<Ticket>().HasQueryFilter(t => !t.IsDeleted);
            modelBuilder.Entity<Manual>().HasQueryFilter(m => !m.IsDeleted);
            modelBuilder.Entity<Usuario>().HasQueryFilter(u => u.IsActive);

            // CORRECCIÓN DE WARNINGS: Filtros en cascada para entidades hijas
            modelBuilder.Entity<Mensaje>().HasQueryFilter(m => !m.Ticket.IsDeleted);
            modelBuilder.Entity<ManualEtiqueta>().HasQueryFilter(e => !e.Manual.IsDeleted);
            modelBuilder.Entity<ManualLog>().HasQueryFilter(log => !log.Manual.IsDeleted);
            modelBuilder.Entity<ManualRolVisibilidad>().HasQueryFilter(rv => !rv.Manual.IsDeleted);

            // ================= :   ===========================================
            // 3. RELACIONES MUCHOS A MUCHOS Y OTROS
            // ============================================================
            modelBuilder.Entity<Usuario>()
                .HasMany(u => u.Categorias)
                .WithMany(c => c.Asesores)
                .UsingEntity<Dictionary<string, object>>(
                    "UsuarioCategoria",
                    j => j.HasOne<Categoria>().WithMany().HasForeignKey("CategoriaId"),
                    j => j.HasOne<Usuario>().WithMany().HasForeignKey("UsuarioId")
                );

            modelBuilder.Entity<ManualEtiqueta>()
                .HasOne(e => e.Manual)
                .WithMany(m => m.ManualEtiquetas)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ManualRolVisibilidad>()
                .HasOne(rv => rv.Manual)
                .WithMany(m => m.RolesVisibles)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}