using System.Data;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Proyecto_GestionGF.Filters;
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

        //Acción que guarda el archivo adjunto de la solicitud en la ruta /adjuntos
        //Con el nombre del ID de la solicitud
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

        [OnlyUserFilter]
        [HttpGet]
        public IActionResult CrearSolicitud()
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            CargarTiposPermiso(cn);
            CargarHeaderUsuario(cn);

            return View(new Solicitud
            {
                FechaInicio = DateTime.Today,
                FechaFinal = DateTime.Today
            });
        }

        [OnlyUserFilter]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearSolicitud(Solicitud solicitud, IFormFile? ArchivoAdjunto)
        {
            using var con = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            // Reforzar IdUsuario desde sesión por seguridad
            solicitud.IdUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;

            if (solicitud.IdUsuario <= 0)
                ModelState.AddModelError("", "Su sesión no es válida. Inicie sesión nuevamente.");

            if (solicitud.IdTipoPermiso <= 0)
                ModelState.AddModelError(nameof(solicitud.IdTipoPermiso), "Debe seleccionar un tipo de permiso.");

            if (solicitud.FechaInicio == default)
                ModelState.AddModelError(nameof(solicitud.FechaInicio), "Debe seleccionar la fecha de inicio.");

            if (solicitud.FechaFinal == default)
                ModelState.AddModelError(nameof(solicitud.FechaFinal), "Debe seleccionar la fecha final.");

            if (solicitud.FechaFinal < solicitud.FechaInicio)
                ModelState.AddModelError(nameof(solicitud.FechaFinal), "La fecha final no puede ser menor a la fecha de inicio.");

            if (string.IsNullOrWhiteSpace(solicitud.Motivo))
                ModelState.AddModelError(nameof(solicitud.Motivo), "Debe ingresar el motivo de la solicitud.");

            // Validación opcional del archivo
            if (ArchivoAdjunto != null && ArchivoAdjunto.Length > 0)
            {
                var ext = Path.GetExtension(ArchivoAdjunto.FileName).ToLowerInvariant();
                var permitidas = new HashSet<string> { ".pdf", ".png", ".jpg", ".jpeg" };

                if (!permitidas.Contains(ext))
                    ModelState.AddModelError("ArchivoAdjunto", "Solo se permiten archivos PDF o imágenes (.png, .jpg, .jpeg).");

                // ejemplo: máximo 5 MB
                if (ArchivoAdjunto.Length > 5 * 1024 * 1024)
                    ModelState.AddModelError("ArchivoAdjunto", "El archivo no puede superar los 5 MB.");
            }

            if (!ModelState.IsValid)
            {
                CargarTiposPermiso(con);
                CargarHeaderUsuario(con);
                return View(solicitud);
            }

            var p = new DynamicParameters();
            p.Add("@IdUsuario", solicitud.IdUsuario);
            p.Add("@IdTipoPermiso", solicitud.IdTipoPermiso);
            p.Add("@FechaInicio", solicitud.FechaInicio);
            p.Add("@FechaFinal", solicitud.FechaFinal);
            p.Add("@Motivo", solicitud.Motivo.Trim());
            p.Add("@ArchivoFile", "");
            p.Add("@EstacionTrabajo", HttpContext.Connection.RemoteIpAddress?.ToString());

            var id = con.QueryFirstOrDefault<int>(
                "CrearSolicitud",
                p,
                commandType: CommandType.StoredProcedure
            );

            if (id <= 0)
            {
                TempData["Error"] = "No se pudo registrar la solicitud.";
                CargarTiposPermiso(con);
                CargarHeaderUsuario(con);
                return View(solicitud);
            }

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
                    TempData["Error"] = "La solicitud fue registrada, pero el archivo no se pudo guardar: " + ex.Message;
                    return RedirectToAction("CrearSolicitud");
                }
            }

            TempData["Ok"] = "Solicitud registrada correctamente.";
            return RedirectToAction("CrearSolicitud");
        }

        //Consulta los tipos de permisos disponibles
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

        //Carga la lista de tipo de permisos, vacaciones, incapacidad, etc..
        private void CargarTiposPermiso(SqlConnection cn)
        {
            var tipos = cn.Query<TipoPermiso>("ConsultaTipoPermisos", commandType: CommandType.StoredProcedure);
            ViewBag.TiposPermiso = new SelectList(tipos, "IdTipoPermiso", "NombrePermiso");
            ViewBag.IdUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
        }

        //Acción para mostrar solicitudes mas recientes
        [HttpGet]
        public IActionResult HistorialAdmin()
        {
            var rol = HttpContext.Session.GetInt32("IdRol");
            if (rol != 1 && rol != 2) return RedirectToAction("MainUsuario", "Home");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var lista = cn.Query<SolicitudHistorialModel>(
                "SolicitudHistorialAdmin",
                commandType: CommandType.StoredProcedure
            ).ToList();

            ViewBag.Nombre = HttpContext.Session.GetString("Nombre") ?? "Administrador";
            CargarNotificacionesNoLeidas(cn);

            return View(lista);
        }

        //Acciones para aprobar o rechazar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Aprobar(int idSolicitud)
        {
            var rol = HttpContext.Session.GetInt32("IdRol");
            if (rol != 1 && rol != 2) return RedirectToAction("MainUsuario", "Home");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var filas = cn.QueryFirstOrDefault<int>(
       "Solicitud_Aprobar",
       new
       {
           IdSolicitud = idSolicitud,
           IdAdmin = HttpContext.Session.GetInt32("IdUsuario"),
           EstacionTrabajo = HttpContext.Connection.RemoteIpAddress?.ToString()
       },
       commandType: CommandType.StoredProcedure
               );

            TempData["Ok"] = filas > 0 ? "Solicitud aprobada." : "No se pudo aprobar (puede que ya no esté pendiente).";
            return RedirectToAction("Main", "Home");
        }
        [HttpGet]
        public IActionResult Rechazar(int id)
        {
            var rol = HttpContext.Session.GetInt32("IdRol");
            if (rol != 1 && rol != 2) return RedirectToAction("MainUsuario", "Home");

            return View(new MotivoRechazoModel { IdSolicitud = id });
        }

        [ValidateAntiForgeryToken]
        public IActionResult Rechazar(MotivoRechazoModel m)
        {
            var rol = HttpContext.Session.GetInt32("IdRol");
            if (rol != 1 && rol != 2) return RedirectToAction("MainUsuario", "Home");

            if (!ModelState.IsValid)
                return View(m);

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var filas = cn.QueryFirstOrDefault<int>(
     "Solicitud_Rechazar",
     new
     {
         IdSolicitud = m.IdSolicitud,
         MotivoRechazo = m.MotivoRechazo,
         IdAdmin = HttpContext.Session.GetInt32("IdUsuario"),
         EstacionTrabajo = HttpContext.Connection.RemoteIpAddress?.ToString()
     },
     commandType: CommandType.StoredProcedure
             );

            TempData["Ok"] = filas > 0 ? "Solicitud rechazada." : "No se pudo rechazar (puede que ya no esté pendiente).";
            return RedirectToAction("Main", "Home");
        }

        //Acción de usuario para cancelar la solicitud HU07
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancelar(int idSolicitud)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            if (idUsuario <= 0) return RedirectToAction("Index", "Home");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var filas = cn.QueryFirstOrDefault<int>(
                "Solicitud_Cancelar",
                new { IdSolicitud = idSolicitud, IdUsuario = idUsuario },
                commandType: CommandType.StoredProcedure
            );

            TempData["Ok"] = filas > 0
                ? "Solicitud cancelada correctamente."
                : "No se pudo cancelar (puede que ya no esté pendiente).";

            return RedirectToAction("MainUsuario", "Home");
        }

        //Se muestra el historial de solicitudes por usuario
        [OnlyUserFilter]
        [HttpGet]
        public IActionResult MisSolicitudes()
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            if (idUsuario <= 0)
                return RedirectToAction("Index", "Home");

            var solicitudes = cn.Query<Solicitud>(
                @"SELECT
            S.IdSolicitud,
            S.IdUsuario,
            S.IdTipoPermiso,
            TP.NombrePermiso AS NombrePermiso,
            S.FechaInicio,
            S.FechaFinal,
            S.Motivo,
            S.ArchivoFile,
            S.Estado,
            S.MotivoRechazo
          FROM Solicitud S
          INNER JOIN TipoPermiso TP ON S.IdTipoPermiso = TP.IdTipoPermiso
          WHERE S.IdUsuario = @IdUsuario
          ORDER BY S.IdSolicitud DESC",
                new { IdUsuario = idUsuario }
            ).ToList();

            CargarHeaderUsuario(cn);

            return View(solicitudes);
        }

        private void CargarNotificacionesNoLeidas(SqlConnection cn)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;

            ViewBag.NotificacionesNoLeidas = cn.QueryFirstOrDefault<int>(
                "Notificacion_ContarNoLeidas",
                new { IdUsuario = idUsuario },
                commandType: CommandType.StoredProcedure
            );
        }

        private void CargarHeaderUsuario(SqlConnection cn)
        {
            ViewBag.Nombre = HttpContext.Session.GetString("Nombre") ?? "Usuario";
            ViewBag.NotificacionesNoLeidas = cn.QueryFirstOrDefault<int>(
                "Notificacion_ContarNoLeidas",
                new { IdUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0 },
                commandType: CommandType.StoredProcedure
            );
        }




    }
}
