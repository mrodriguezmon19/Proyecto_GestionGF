namespace Proyecto_GestionGF.Models
{
    public class AuditoriaModel
    {
        public int IdAuditoria { get; set; }
        public int? IdSolicitud { get; set; }
        public int? IdUsuario { get; set; }
        public int? IdRol { get; set; }
        public int? IdDepartamento { get; set; }
        public DateTime FechaCambio { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string? Resultado { get; set; }
        public string? MotivoRechazo { get; set; }
        public string? EstacionTrabajo { get; set; }

        public string? NombreUsuario { get; set; }
        public string? NombreRol { get; set; }
        public string? NombreDepartamento { get; set; }
    }
}
