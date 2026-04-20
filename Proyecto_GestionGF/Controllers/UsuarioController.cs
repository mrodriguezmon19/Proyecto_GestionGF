using System.Data;
using Dapper;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Proyecto_GestionGF.Filters;
using Proyecto_GestionGF.Models;

namespace Proyecto_GestionGF.Controllers
{
    [OnlyAdminFilter]
    public class UsuarioController : Controller
    {
        private readonly IConfiguration _configuration;

        public UsuarioController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var lista = cn.Query<Usuario>(
                "Usuario_Listar",
                commandType: CommandType.StoredProcedure
            ).ToList();

            ViewBag.Nombre = HttpContext.Session.GetString("Nombre") ?? "Administrador";

            return View(lista);
        }

        [HttpGet]
        public IActionResult Create()
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            CargarCombos(cn);

            return View(new Usuario { Activo = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Usuario model)
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            if (!ModelState.IsValid)
            {
                CargarCombos(cn);
                return View(model);
            }

            var existe = cn.QueryFirstOrDefault<int>(
                "Usuario_Existe",
                new
                {
                    NombreUsuario = model.NombreUsuario,
                    CorreoElectronico = model.CorreoElectronico
                },
                commandType: CommandType.StoredProcedure
            );

            if (existe > 0)
            {
                ModelState.AddModelError("", "Ya existe un usuario con ese nombre de usuario o correo.");
                CargarCombos(cn);
                return View(model);
            }

            var id = cn.QueryFirstOrDefault<int>(
                "Usuario_Registrar",
                new
                {
                    Identificacion = model.Identificacion,
                    Nombre = model.Nombre,
                    NombreUsuario = model.NombreUsuario,
                    Password = model.Password,
                    CorreoElectronico = model.CorreoElectronico,
                    IdRol = model.IdRol,
                    IdDepartamento = model.IdDepartamento,
                    Activo = model.Activo
                },
                commandType: CommandType.StoredProcedure
            );

            TempData["Ok"] = id > 0
                ? "Usuario registrado correctamente."
                : "No se pudo registrar el usuario.";

            return RedirectToAction("Index");
        }

        private void CargarCombos(SqlConnection cn)
        {
            var roles = cn.Query<Rol>(
                "Rol_Listar",
                commandType: CommandType.StoredProcedure
            ).ToList();

            var departamentos = cn.Query<Departamento>(
                "Departamento_Listar",
                commandType: CommandType.StoredProcedure
            ).ToList();

            ViewBag.Roles = new SelectList(roles, "IdRol", "NombreRol");
            ViewBag.Departamentos = new SelectList(departamentos, "IdDepartamento", "NombreDepartamento");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var model = cn.QueryFirstOrDefault<Usuario>(
                "Usuario_ConsultarPorId",
                new { IdUsuario = id },
                commandType: CommandType.StoredProcedure
            );

            if (model == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            CargarCombos(cn);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Usuario model)
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            if (!ModelState.IsValid)
            {
                CargarCombos(cn);
                return View(model);
            }

            var existe = cn.QueryFirstOrDefault<int>(
                "Usuario_ExisteEditar",
                new
                {
                    IdUsuario = model.IdUsuario,
                    NombreUsuario = model.NombreUsuario,
                    CorreoElectronico = model.CorreoElectronico
                },
                commandType: CommandType.StoredProcedure
            );

            if (existe > 0)
            {
                ModelState.AddModelError("", "Ya existe otro usuario con ese nombre de usuario o correo.");
                CargarCombos(cn);
                return View(model);
            }

            var filas = cn.QueryFirstOrDefault<int>(
                "Usuario_Actualizar",
                new
                {
                    IdUsuario = model.IdUsuario,
                    Identificacion = model.Identificacion,
                    Nombre = model.Nombre,
                    NombreUsuario = model.NombreUsuario,
                    Password = model.Password,
                    CorreoElectronico = model.CorreoElectronico,
                    IdRol = model.IdRol,
                    IdDepartamento = model.IdDepartamento,
                    Activo = model.Activo
                },
                commandType: CommandType.StoredProcedure
            );

            TempData["Ok"] = filas > 0
                ? "Usuario actualizado correctamente."
                : "No se pudo actualizar el usuario.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Desbloquear(int idUsuario)
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var filas = cn.QueryFirstOrDefault<int>(
                "Usuario_Desbloquear",
                new { IdUsuario = idUsuario },
                commandType: CommandType.StoredProcedure
            );

            TempData["Ok"] = filas > 0
                ? "Usuario desbloqueado correctamente."
                : "No se pudo desbloquear el usuario.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarEstado(int idUsuario, bool activo)
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var filas = cn.QueryFirstOrDefault<int>(
                "Usuario_CambiarEstado",
                new
                {
                    IdUsuario = idUsuario,
                    Activo = activo
                },
                commandType: CommandType.StoredProcedure
            );

            TempData["Ok"] = filas > 0
                ? (activo ? "Usuario activado correctamente." : "Usuario inactivado correctamente.")
                : "No se pudo cambiar el estado del usuario.";

            return RedirectToAction("Index");
        }
    }
}
