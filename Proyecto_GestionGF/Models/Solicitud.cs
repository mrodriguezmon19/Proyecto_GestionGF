

namespace Proyecto_GestionGF.Models
{
    public class Solicitud
    {

        public int IdSolicitud { get; set; }

        public int IdUsuario { get; set; }
        
        public int IdTipoPermiso {  get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFinal {  get; set; }

        public string Motivo { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public string ArchivoFile { get; set; } = string.Empty;

    }
}
