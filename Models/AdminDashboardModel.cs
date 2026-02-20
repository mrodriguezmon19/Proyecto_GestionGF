using System.Collections.Generic;

namespace Proyecto_GestionGF.Models
{
    public class AdminDashboardModel
    {
        public string Nombre { get; set; } = string.Empty;

        public int NotificacionesNoLeidas { get; set; }

        public List<SolicitudRow> Solicitudes { get; set; } = new();
    }
}