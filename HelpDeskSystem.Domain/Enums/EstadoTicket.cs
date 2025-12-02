using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpDeskSystem.Domain.Enums
{
    public enum EstadoTicket
    {
        Abierto = 1,
        Asignado = 2,
        EnProgreso = 3,
        Resuelto = 4,
        Cancelado = 5
    }
}
