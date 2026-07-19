using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Proyecto_GestionGF.Models;

namespace Proyecto_GestionGF.Controllers
{
    public class NotificacionController : Controller
    {
        private readonly IConfiguration _configuration;

        public NotificacionController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private void CargarDatosHeader(SqlConnection cn, int idUsuario)
        {
            ViewBag.Nombre = HttpContext.Session.GetString("Nombre") ?? "Usuario";

            ViewBag.NotificacionesNoLeidas = cn.QueryFirstOrDefault<int>(
                "Notificacion_ContarNoLeidas",
                new { IdUsuario = idUsuario },
                commandType: CommandType.StoredProcedure
            );
        }

        [HttpGet]
        public IActionResult Index()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            if (idUsuario <= 0)
                return RedirectToAction("Index", "Home");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var lista = cn.Query<NotificacionModel>(
                "Notificacion_Listar",
                new { IdUsuario = idUsuario, Top = 50 },
                commandType: CommandType.StoredProcedure
            ).ToList();

            CargarDatosHeader(cn, idUsuario);

            ViewBag.NoLeidas = ViewBag.NotificacionesNoLeidas;

            return View(lista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarcarLeida(int idNotificacion)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            if (idUsuario <= 0)
                return RedirectToAction("Index", "Home");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            cn.QueryFirstOrDefault<int>(
                "Notificacion_MarcarLeida",
                new { IdNotificacion = idNotificacion, IdUsuario = idUsuario },
                commandType: CommandType.StoredProcedure
            );

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarcarTodasLeidas()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            if (idUsuario <= 0)
                return RedirectToAction("Index", "Home");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            cn.QueryFirstOrDefault<int>(
                "Notificacion_MarcarTodasLeidas",
                new { IdUsuario = idUsuario },
                commandType: CommandType.StoredProcedure
            );

            return RedirectToAction("Index");
        }
    }
}

