using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpDeskSystem.Domain.Entities
{
    public class ManualEtiqueta
    {
        [Key]
        public int Id { get; set; }

        public int ManualId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Etiqueta { get; set; } = string.Empty;

        [ForeignKey("ManualId")]
        public Manual? Manual { get; set; }
    }
}