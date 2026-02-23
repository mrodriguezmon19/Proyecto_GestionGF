namespace Proyecto_GestionGF.Models
{
    public class AdminDashboardModel
    {
        public string Nombre { get; set; } = "";
        public List<AdminSolicitudRow> Solicitudes { get; set; } = new();

        public int NotificacionesNoLeidas { get; set; }
        public List<NotificacionModel> Notificaciones { get; set; } = new();
    }

    public class AdminSolicitudRow
    {
        public int IdSolicitud { get; set; }
        public string Colaborador { get; set; } = "";
        public string TipoPermiso { get; set; } = "";
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFinal { get; set; }
        public byte Estado { get; set; }

        public string Motivo { get; set; } = string.Empty;
        public string NombreEstado { get; set; } = "";
        public string ArchivoFile { get; set; } = "";

        public string? MotivoRechazo { get; set; } = string.Empty;
    }
}
