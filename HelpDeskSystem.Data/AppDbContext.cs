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
        public DbSet<ManualLog> ManualLogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // SE ELIMINÓ LA CADENA DE CONEXIÓN HARDCODED
            // Ahora la aplicación utilizará la configuración inyectada desde Program.cs (appsettings.json).
            // Si necesitas ejecutar migraciones manualmente desde consola sin iniciar la app, 
            // usa el parámetro --connection o configura los User Secrets.
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. Configuración de Ticket
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Usuario)
                .WithMany()
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. SEMILLA DE CATEGORÍAS (Estructurales del sistema)
            // Se mantienen para que el sistema no esté vacío de opciones al iniciar.
            var catFinanzas = new Categoria { Id = 1, Nombre = "Sistema Financiero" };
            var catGenesis = new Categoria { Id = 2, Nombre = "Sistema Genesis" };
            var catReportes = new Categoria { Id = 3, Nombre = "Reportes" };

            modelBuilder.Entity<Categoria>().HasData(catFinanzas, catGenesis, catReportes);

            // 3. USUARIO ADMINISTRADOR (Único usuario inicial)
            // Necesario para poder hacer Login y crear a los demás usuarios desde la App.
            var idAdmin = Guid.Parse("11111111-1111-1111-1111-111111111111");

            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = idAdmin,
                    Nombre = "Administrador Jefe",
                    Email = "admin@helpdesk.com",
                    Rol = RolUsuario.Administrador,
                    // Password es "admin" encriptado con BCrypt
                    Password = BCrypt.Net.BCrypt.HashPassword("$50pt3Uteps4"),
                    IsActive = true
                }
            );

            // 4. RELACIÓN MUCHOS A MUCHOS (ASESORES <-> CATEGORÍAS)
            // Definimos la estructura de la tabla intermedia sin insertar datos falsos.
            modelBuilder.Entity<Usuario>()
                .HasMany(u => u.Categorias)
                .WithMany(c => c.Asesores)
                .UsingEntity<Dictionary<string, object>>(
                    "UsuarioCategoria",
                    j => j.HasOne<Categoria>().WithMany().HasForeignKey("CategoriasId"),
                    j => j.HasOne<Usuario>().WithMany().HasForeignKey("AsesoresId")
                );

            // 5. Configuración de ManualLog
            modelBuilder.Entity<ManualLog>()
                .HasOne(log => log.Usuario)
                .WithMany()
                .HasForeignKey(log => log.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict); // Protege la auditoría
        }
    }
}