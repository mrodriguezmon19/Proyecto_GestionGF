using System;

namespace Proyecto_GestionGF.Models
{
    public class SolicitudRow
    {
        public int IdSolicitud { get; set; }

        public string NombreUsuario { get; set; } = string.Empty;

        public string NombrePermiso { get; set; } = string.Empty;

        public string Motivo { get; set; } = string.Empty;

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFinal { get; set; }

        public int Estado { get; set; }

        public string? MotivoRechazo { get; set; }

        public string? ArchivoFile { get; set; }
    }
}
