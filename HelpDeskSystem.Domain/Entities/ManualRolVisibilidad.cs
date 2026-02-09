using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpDeskSystem.Domain.Entities
{
    public class ManualRolVisibilidad
    {
        [Key]
        public int Id { get; set; }

        public int ManualId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RolNombre { get; set; } = string.Empty; // Ej: "Administrador", "Asesor"

        [ForeignKey("ManualId")]
        public Manual? Manual { get; set; }
    }
}