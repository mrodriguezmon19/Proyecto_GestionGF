using System;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_GestionGF.Models
{
    public class Solicitud
    {
        public int IdSolicitud { get; set; }

        public int IdUsuario { get; set; }

        public int IdTipoPermiso { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFinal { get; set; }

        [Required]
        public string Motivo { get; set; } = string.Empty;

        public int Estado { get; set; }

        public string? MotivoRechazo { get; set; }

        public string? ArchivoFile { get; set; }
    }
}