namespace Proyecto_GestionGF.Models
{
    public class NotificacionModel
    {
        public int IdNotificacion { get; set; }
        public int IdUsuario { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public bool Leida { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}

