using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Proyecto_GestionGF.Models;
using System.Data;

namespace Proyecto_GestionGF.Controllers
{
    public class SolicitudController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        public SolicitudController(IConfiguration configuration, IHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        // ====== 1) Guardar imagen como {IdSolicitud}.png en wwwroot/imagenes ======
        private string GuardarDatosImagen(IFormFile? archivo, int idSolicitud)
        {
            if (archivo == null || archivo.Length == 0) return string.Empty;

            var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            // Fuerza extensión .png como en el ejemplo del profesor:
            var nombre = idSolicitud + ".png";
            var rutaFisica = Path.Combine(carpeta, nombre);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                archivo.CopyTo(stream);
            }

            // ruta relativa para servir desde la web:
            return "/imagenes/" + nombre;
        }

        [HttpGet]
        public IActionResult CrearSolicitud()
        {
            return View(new Solicitud
            {
                FechaInicio = DateTime.Today,
                FechaFinal = DateTime.Today
            });
        }

        // ====== 2) Crear + guardar imagen + actualizar ruta ======
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearSolicitud(Solicitud solicitud, IFormFile? ArchivoAdjunto)
        {
            using (var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]))
            {
                // PASO A: Crear solicitud y recibir el Id generado (sin ruta todavía)
                var pCrear = new DynamicParameters();
                pCrear.Add("@IdUsuario", solicitud.IdUsuario);
                pCrear.Add("@IdTipoPermiso", solicitud.IdTipoPermisos);
                pCrear.Add("@FechaInicio", solicitud.FechaInicio);
                pCrear.Add("@FechaFinal", solicitud.FechaFinal);
                pCrear.Add("@Motivo", solicitud.Motivo);

                // IMPORTANTE: que el SP retorne el IdSolicitud (SELECT SCOPE_IDENTITY())
                var idGenerado = context.QueryFirstOrDefault<int>(
                    "CrearSolicitud",
                    pCrear,
                    commandType: CommandType.StoredProcedure
                );

                if (idGenerado <= 0)
                {
                    ViewBag.Ok = false;
                    ViewBag.Mensaje = "La solicitud no ha sido registrada";
                    return View(solicitud);
                }

                // PASO B: Guardar imagen como {IdSolicitud}.png
                string rutaRelativa = string.Empty;
                if (ArchivoAdjunto != null && ArchivoAdjunto.Length > 0)
                {
                    rutaRelativa = GuardarDatosImagen(ArchivoAdjunto, idGenerado);
                }

                // PASO C: Actualizar la ruta en la solicitud (si hubo archivo)
                if (!string.IsNullOrWhiteSpace(rutaRelativa))
                {
                    var pUpd = new DynamicParameters();
                    pUpd.Add("@IdSolicitud", idGenerado);
                    pUpd.Add("@ArchivoFile", rutaRelativa);

                    // Crea un SP o un UPDATE directo según prefieras
                    context.Execute(
                        "Solicitud_ActualizarArchivo",
                        pUpd,
                        commandType: CommandType.StoredProcedure
                    );
                }

                ViewBag.Ok = true;
                ViewBag.Mensaje = "Solicitud registrada correctamente!";
                ModelState.Clear();
                return View(new Solicitud
                {
                    FechaInicio = DateTime.Today,
                    FechaFinal = DateTime.Today
                });
            }
        }
    }
}
