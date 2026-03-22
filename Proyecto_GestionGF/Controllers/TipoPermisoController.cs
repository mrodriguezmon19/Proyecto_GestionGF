using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Proyecto_GestionGF.Models;

namespace Proyecto_GestionGF.Controllers
{
    public class TipoPermisoController : Controller
    {
        private readonly IConfiguration _configuration;

        public TipoPermisoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetInt32("IdRol");
            if (rol != 1 && rol != 2)
                return RedirectToAction("MainUsuario", "Home");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var lista = cn.Query<TipoPermiso>(
                "ListaTipoPermisos",
                commandType: CommandType.StoredProcedure
            ).ToList();

            ViewBag.Nombre = HttpContext.Session.GetString("Nombre") ?? "Administrador";

            return View(lista);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var rol = HttpContext.Session.GetInt32("IdRol");
            if (rol != 1 && rol != 2)
                return RedirectToAction("MainUsuario", "Home");

            return View(new TipoPermiso());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(TipoPermiso model)
        {
            var rol = HttpContext.Session.GetInt32("IdRol");
            if (rol != 1 && rol != 2)
                return RedirectToAction("MainUsuario", "Home");

            if (!ModelState.IsValid)
                return View(model);

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var id = cn.QueryFirstOrDefault<int>(
                "RegistrarPermiso",
                new
                {
                    NombrePermiso = model.NombrePermiso,
                    Descripcion = model.Descripcion
                },
                commandType: CommandType.StoredProcedure
            );

            TempData["Ok"] = id > 0
                ? "Tipo de permiso registrado correctamente."
                : "No se pudo registrar el tipo de permiso.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var rol = HttpContext.Session.GetInt32("IdRol");
            if (rol != 1 && rol != 2)
                return RedirectToAction("MainUsuario", "Home");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var model = cn.QueryFirstOrDefault<TipoPermiso>(
                "TipoPermisoConsultarPorId",
                new { IdTipoPermiso = id },
                commandType: CommandType.StoredProcedure
            );

            if (model == null)
            {
                TempData["Error"] = "Tipo de permiso no encontrado.";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(TipoPermiso model)
        {
            var rol = HttpContext.Session.GetInt32("IdRol");
            if (rol != 1 && rol != 2)
                return RedirectToAction("MainUsuario", "Home");

            if (!ModelState.IsValid)
                return View(model);

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var filas = cn.QueryFirstOrDefault<int>(
                "ActualizarPermiso",
                new
                {
                    IdTipoPermiso = model.IdTipoPermiso,
                    NombrePermiso = model.NombrePermiso,
                    Descripcion = model.Descripcion
                },
                commandType: CommandType.StoredProcedure
            );

            TempData["Ok"] = filas > 0
                ? "Tipo de permiso actualizado correctamente."
                : "No se pudo actualizar el tipo de permiso.";

            return RedirectToAction("Index");
        }
    }
}
