using System.Data;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Proyecto_GestionGF.Models;

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


        private void GuardarDatosImagenConConsecutivo(IFormFile Imagen, int ConsecutivoProducto)
        {
            if (Imagen != null)
            {
                //save imagen 
                var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");

                if (!Directory.Exists(carpeta))
                    Directory.CreateDirectory(carpeta);

                var nombreImagen = ConsecutivoProducto + ".png";
                var carpetaFinal = Path.Combine(carpeta, nombreImagen);

                using (var stream = new FileStream(carpetaFinal, FileMode.Create))
                {
                    Imagen.CopyTo(stream);
                }
            }
        }

        private string GuardarArchivo(IFormFile archivo, int idSolicitud)
        {
            if (archivo == null || archivo.Length == 0) return string.Empty;

            var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var permitidas = new HashSet<string> { ".pdf", ".png", ".jpg", ".jpeg" };

            if (!permitidas.Contains(ext))
                throw new InvalidOperationException("Formato no permitido. Solo PDF o imágenes (png/jpg).");

            var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "adjuntos");
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            var nombre = $"{idSolicitud}{ext}";
            var rutaFisica = Path.Combine(carpeta, nombre);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                archivo.CopyTo(stream);
            }

            return "/adjuntos/" + nombre;
        }


        [HttpGet]
        public IActionResult CrearSolicitud()
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            CargarTiposPermiso(cn);

            return View(new Solicitud
            {
                FechaInicio = DateTime.Today,
                FechaFinal = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearSolicitud(Solicitud solicitud, IFormFile? ArchivoAdjunto)
        {
            using var con = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            // Validación simple (opcional pero recomendable)
            if (solicitud.IdUsuario <= 0)
                ModelState.AddModelError("", "Usuario inválido. Inicie sesión nuevamente.");

            if (solicitud.IdTipoPermiso <= 0)
                ModelState.AddModelError(nameof(solicitud.IdTipoPermiso), "Seleccione un tipo de permiso.");

            if (solicitud.FechaFinal < solicitud.FechaInicio)
                ModelState.AddModelError(nameof(solicitud.FechaFinal), "La fecha final no puede ser menor a la fecha de inicio.");

            if (!ModelState.IsValid)
            {
                CargarTiposPermiso(con);
                return View(solicitud);
            }

            // Crear solicitud sin ruta de archivo aún no tenemos IdSolicitud para relacionarla
            var p = new DynamicParameters();
            p.Add("@IdUsuario", solicitud.IdUsuario);
            p.Add("@IdTipoPermiso", solicitud.IdTipoPermiso);
            p.Add("@FechaInicio", solicitud.FechaInicio);
            p.Add("@FechaFinal", solicitud.FechaFinal);
            p.Add("@Motivo", solicitud.Motivo);
            p.Add("@ArchivoFile", ""); // vacío temporal

            var id = con.QueryFirstOrDefault<int>("CrearSolicitud", p, commandType: CommandType.StoredProcedure);

            if (id <= 0)
            {
                TempData["Error"] = "No se pudo registrar la solicitud.";
                CargarTiposPermiso(con);
                return View(solicitud);
            }

            // Si se adjunto archivo, se guarda y actualiza ruta en BD
            if (ArchivoAdjunto != null && ArchivoAdjunto.Length > 0)
            {
                try
                {
                    var ruta = GuardarArchivo(ArchivoAdjunto, id);

                    var pUpd = new DynamicParameters();
                    pUpd.Add("@IdSolicitud", id);
                    pUpd.Add("@ArchivoFile", ruta);

                    con.Execute("Solicitud_ActualizarArchivo", pUpd, commandType: CommandType.StoredProcedure);
                }
                catch (Exception ex)
                {
                    // La solicitud ya está creada, pero el archivo falló
                    TempData["Error"] = "Solicitud creada, pero el archivo no se pudo guardar: " + ex.Message;
                    return RedirectToAction("CrearSolicitud");
                }
            }

            TempData["Ok"] = "Solicitud registrada correctamente.";
            return RedirectToAction("CrearSolicitud");
        }


        [HttpGet]
        public IActionResult ConsultaTipoPermisos()
        {
            using (var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]))
            {
                var parametros = new DynamicParameters();
                var resultado = context.Query<TipoPermiso>("ConsultaTipoPermisos", parametros);

                return Ok(resultado);
            }
        }

        private void CargarTiposPermiso(SqlConnection cn)
        {
            var tipos = cn.Query<TipoPermiso>("ConsultaTipoPermisos", commandType: CommandType.StoredProcedure);
            ViewBag.TiposPermiso = new SelectList(tipos, "IdTipoPermiso", "NombrePermiso");
            ViewBag.IdUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
        }

        [HttpGet]
        public IActionResult HistorialAdmin()
        {
            var rol = HttpContext.Session.GetInt32("ConsecutivoPerfil");
            if (rol != 1 && rol != 2) return RedirectToAction("MainUsuario", "Home");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var lista = cn.Query<SolicitudHistorialModel>(
                "Solicitud_Historial_Admin",
                commandType: CommandType.StoredProcedure
            ).ToList();

            return View(lista);
        }



    }
}
