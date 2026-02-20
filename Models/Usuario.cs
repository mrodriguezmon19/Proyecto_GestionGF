namespace Proyecto_GestionGF.Models
{
    public class Usuario
    {

        public int IdUsuario {  get; set; }

        public string Identificacion { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string NombreUsuario { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string CorreoElectronico { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; }

        public int IdRol { get; set; }

        public int IdDepartamento { get; set; }

        public bool Activo {  get; set; }

        public string NombreRol { get; set; } = string.Empty;

        public string NombreDepartamento { get; set; } = string.Empty;


    }
}
