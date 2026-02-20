using System.ComponentModel.DataAnnotations;

namespace Proyecto_GestionGF.Models
{
    public class MotivoRechazoModel
    {
        public int IdSolicitud { get; set; }

        [Required(ErrorMessage = "Se debe indicar el motivo del rechazo.")]
        [StringLength(500)]
        public string MotivoRechazo { get; set; } = string.Empty;
    }
}
