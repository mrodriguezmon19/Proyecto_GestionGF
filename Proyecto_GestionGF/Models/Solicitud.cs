using System.ComponentModel.DataAnnotations;

namespace Proyecto_GestionGF.Models
{
    public class Solicitud
    {
        public int IdSolicitud { get; set; }

        public int IdUsuario { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un tipo de permiso.")]
        public int IdTipoPermiso { get; set; }

        public string NombrePermiso { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime FechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha final es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime FechaFinal { get; set; }

        [Required(ErrorMessage = "Debe ingresar el motivo de la solicitud.")]
        [StringLength(500, ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
        public string Motivo { get; set; } = string.Empty;

        public string ArchivoFile { get; set; } = string.Empty;

        public int Estado { get; set; }

        public string? MotivoRechazo { get; set; }
    }
}
