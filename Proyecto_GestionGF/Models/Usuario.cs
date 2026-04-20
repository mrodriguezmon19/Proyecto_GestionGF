using System.ComponentModel.DataAnnotations;

namespace Proyecto_GestionGF.Models
{
    public class Usuario
    {
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "La identificación es obligatoria.")]
        public string Identificacion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Debe ingresar un correo válido.")]
        public string CorreoElectronico { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un rol.")]
        public int IdRol { get; set; }

        public string NombreRol { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un departamento.")]
        public int IdDepartamento { get; set; }

        public string NombreDepartamento { get; set; } = string.Empty;

        public bool Activo { get; set; }

        public int IntentosFallidos { get; set; }
        public bool Bloqueado { get; set; }
        public DateTime? FechaBloqueo { get; set; }
    }
}
