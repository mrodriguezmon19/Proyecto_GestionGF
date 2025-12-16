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


        private void GuardarDatosImagen(IFormFile Imagen, int ConsecutivoProducto)
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

        [HttpGet]
        public IActionResult CrearSolicitud()
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var tipos = cn.Query<TipoPermiso>("ConsultaTipoPermisos",
                                             commandType: CommandType.StoredProcedure);

            ViewBag.TiposPermiso = new SelectList(tipos, "IdTipoPermiso", "NombrePermiso");

            ViewBag.IdUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;

            return View(new Solicitud
            {
                FechaInicio = DateTime.Today,
                FechaFinal = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearSolicitud(Solicitud solicitud, IFormFile? ArchivoFile)
        {

            solicitud.ArchivoFile = "/imagenes/";
            using var con = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            
            var p = new DynamicParameters();
            p.Add("@IdUsuario", solicitud.IdUsuario);
            p.Add("@IdTipoPermiso", solicitud.IdTipoPermiso);
            p.Add("@FechaInicio", solicitud.FechaInicio);
            p.Add("@FechaFinal", solicitud.FechaFinal);
            p.Add("@Motivo", solicitud.Motivo);
            p.Add("@ArchivoFile", solicitud.ArchivoFile);

            var id = con.QueryFirstOrDefault<int>("CrearSolicitud", p, commandType: CommandType.StoredProcedure);

            if (id <= 0)
            {
                TempData["Error"] = "No se pudo registrar la solicitud.";
                return View(solicitud);
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

    }
}
