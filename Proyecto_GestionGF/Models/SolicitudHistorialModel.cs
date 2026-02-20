namespace Proyecto_GestionGF.Models
{
    public class SolicitudHistorialModel
    {
        public int IdSolicitud { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string NombrePermiso { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFinal { get; set; }
        public string? MotivoRechazo { get; set; } = string.Empty;

        public byte Estado { get; set; } // 0=Pendiente,1=Aprobado,2=Rechazado
        public string ArchivoFile { get; set; } = string.Empty;
    }
}