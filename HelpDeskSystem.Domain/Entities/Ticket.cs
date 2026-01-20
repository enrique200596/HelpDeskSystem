using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Domain.Interfaces;

namespace HelpDeskSystem.Domain.Entities
{
    public class Ticket : ISoftDelete
    {
        public int Id { get; set; }

        public Guid UsuarioId { get; set; }
        public virtual Usuario? Usuario { get; set; }

        public Guid? AsesorId { get; set; }
        public virtual Usuario? Asesor { get; set; }

        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        public EstadoTicket Estado { get; set; } = EstadoTicket.Abierto;
        public bool EsUrgente { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaCierre { get; set; }

        public bool FueEditado { get; set; } = false;

        public int? SatisfaccionUsuario { get; set; }

        public bool IsDeleted { get; set; } = false;

        public int CategoriaId { get; set; }
        public virtual Categoria? Categoria { get; set; }

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