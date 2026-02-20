namespace Proyecto_GestionGF.Models
{
    public class UsuarioDashboardModel
    {
        public string Nombre { get; set; } = "Usuario";
        public List<UsuarioSolicitudRow> UltimasSolicitudes { get; set; } = new();

        public int NotificacionesNoLeidas { get; set; }
        public List<NotificacionModel> Notificaciones { get; set; } = new();
    }

    public class UsuarioSolicitudRow
    {
        public int IdSolicitud { get; set; }
        public string TipoPermiso { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFinal { get; set; }
        public byte Estado { get; set; }
        public string NombreEstado { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public string? ArchivoFile { get; set; }
        public string? MotivoRechazo { get; set; }
    }
}

