

using System.ComponentModel.DataAnnotations;

namespace Proyecto_GestionGF.Models
{
    public class Solicitud
    {

        public int IdSolicitud { get; set; }

        public int IdUsuario { get; set; }
        
        public int IdTipoPermiso {  get; set; }

        [Required(ErrorMessage = "Se debe indicar la fecha de inicio.")]
        public DateTime FechaInicio { get; set; }

        [Required(ErrorMessage = "Se debe indicar la fecha final.")]
        public DateTime FechaFinal {  get; set; }

        [Required(ErrorMessage = "Se debe indicar el motivo del permiso.")]
        public string Motivo { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public string ArchivoFile { get; set; } = string.Empty;

    }
}
