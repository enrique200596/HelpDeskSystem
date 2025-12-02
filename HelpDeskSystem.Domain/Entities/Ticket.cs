using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Domain.Entities
{
    public class Ticket
    {
        // En C#, usamos Propiedades (get; set;) en lugar de campos públicos.
        public int Id { get; set; }

        // --- RELACIÓN CON EL CREADOR ---
        public Guid UsuarioId { get; set; }
        // Esta propiedad "virtual" es la conexión para EF Core
        public virtual Usuario? Usuario { get; set; }
        // --- RELACIÓN CON EL ASESOR ---
        public Guid? AsesorId { get; set; }
        public virtual Usuario? Asesor { get; set; }
        public string Titulo { get; set; } = string.Empty; // Inicializamos para evitar nulos
        public string Descripcion { get; set; } = string.Empty;

        public EstadoTicket Estado { get; set; } = EstadoTicket.Abierto;
        public bool EsUrgente { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // NUEVA PROPIEDAD: Para métricas de tiempo
        public DateTime? FechaCierre { get; set; }

        // Control de edición única que pediste
        public bool FueEditado { get; set; } = false;

        // Métricas: Satisfacción (puede ser nulo si aun no calificó)
        public int? SatisfaccionUsuario { get; set; }

        public bool IsDeleted { get; set; } = false; // Soft delete
        // Nueva Relación
        public int CategoriaId { get; set; }
        public virtual Categoria? Categoria { get; set; }

        // Lógica simple de dominio dentro de la entidad
        public void AsignarAsesor(Guid nuevoAsesorId)
        {
            AsesorId = nuevoAsesorId;
            Estado = EstadoTicket.Asignado;
        }

        public void CerrarTicket(int satisfaccion)
        {
            if (satisfaccion < 1 || satisfaccion > 5)
                throw new ArgumentException("La satisfacción debe ser entre 1 y 5");

            Estado = EstadoTicket.Resuelto;
            SatisfaccionUsuario = satisfaccion;
        }
    }
}
