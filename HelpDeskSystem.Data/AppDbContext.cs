using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=srv-sistem-saf;Database=HelpDeskDB;User Id=sa;Password=$Sin123$5;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuración de Ticket
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Usuario)
                .WithMany()
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. SEMILLA DE CATEGORÍAS
            var catFinanzas = new Categoria { Id = 1, Nombre = "Sistema Financiero" };
            var catGenesis = new Categoria { Id = 2, Nombre = "Sistema Genesis" };
            var catReportes = new Categoria { Id = 3, Nombre = "Reportes" };

            modelBuilder.Entity<Categoria>().HasData(catFinanzas, catGenesis, catReportes);

            // 3. USUARIOS (Tus usuarios fijos)
            var idAdmin = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var idJuan = Guid.Parse("22222222-2222-2222-2222-222222222222"); // Asesor
            var idCarla = Guid.Parse("33333333-3333-3333-3333-333333333333"); // Usuario
            var idAna = Guid.Parse("44444444-4444-4444-4444-444444444444"); // Asesor
            var idPedro = Guid.Parse("55555555-5555-5555-5555-555555555555"); // Usuario

            modelBuilder.Entity<Usuario>().HasData(
                new Usuario { Id = idAdmin, Nombre = "Administrador Jefe", Email = "admin@helpdesk.com", Rol = RolUsuario.Administrador, Password = BCrypt.Net.BCrypt.HashPassword("admin"), IsActive = true },
                new Usuario { Id = idJuan, Nombre = "Juan Asesor", Email = "juan@helpdesk.com", Rol = RolUsuario.Asesor, Password = BCrypt.Net.BCrypt.HashPassword("1234"), IsActive = true },
                new Usuario { Id = idCarla, Nombre = "Carla Usuario", Email = "carla@cliente.com", Rol = RolUsuario.Usuario, Password = BCrypt.Net.BCrypt.HashPassword("1234"), IsActive = true },
                new Usuario { Id = idAna, Nombre = "Ana Asesora", Email = "ana@helpdesk.com", Rol = RolUsuario.Asesor, Password = BCrypt.Net.BCrypt.HashPassword("1234"), IsActive = true },
                new Usuario { Id = idPedro, Nombre = "Pedro Cliente", Email = "pedro@cliente.com", Rol = RolUsuario.Usuario, Password = BCrypt.Net.BCrypt.HashPassword("1234"), IsActive = true }
            );

            // 4. RELACIÓN MUCHOS A MUCHOS (ASESORES <-> CATEGORÍAS)
            // Esto es lo que faltaba: Asignar las especialidades a cada asesor
            modelBuilder.Entity<Usuario>().HasMany(u => u.Categorias).WithMany(c => c.Asesores).UsingEntity<Dictionary<string, object>>(
                "UsuarioCategoria", // Nombre de la tabla intermedia
                j => j.HasOne<Categoria>().WithMany().HasForeignKey("CategoriasId"),
                j => j.HasOne<Usuario>().WithMany().HasForeignKey("AsesoresId"),
                j =>
                {
                    // AQUÍ ASIGNAMOS:
                    j.HasData(
                        // Juan atiende: Finanzas (1) y Genesis (2)
                        new { AsesoresId = idJuan, CategoriasId = 1 },
                        new { AsesoresId = idJuan, CategoriasId = 2 },
                        new { AsesoresId = idAna, CategoriasId = 3 }
                    );
                }
            );
            base.OnModelCreating(modelBuilder);
        }
    }
}