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

        //Accion que muestra el menú de las notificaciones
        [HttpGet]
        public IActionResult Index()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            if (idUsuario <= 0) return RedirectToAction("Index", "Home");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var lista = cn.Query<NotificacionModel>(
                "Notificacion_Listar",
                new { IdUsuario = idUsuario, Top = 50 },
                commandType: CommandType.StoredProcedure
            ).ToList();

            ViewBag.NoLeidas = cn.QueryFirstOrDefault<int>(
                "Notificacion_ContarNoLeidas",
                new { IdUsuario = idUsuario },
                commandType: CommandType.StoredProcedure
            );

            return View(lista); // Views/Notificacion/Index.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarcarLeida(int idNotificacion)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            if (idUsuario <= 0) return RedirectToAction("Index", "Home");
        //Accion para marcar como leida la notificación

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            cn.QueryFirstOrDefault<int>(
                "Notificacion_MarcarLeida",
                new { IdNotificacion = idNotificacion, IdUsuario = idUsuario },
                commandType: CommandType.StoredProcedure
            );

            return RedirectToAction("Index");
        }


        //Accion para marcar como leidas las notificaciones
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarcarTodasLeidas()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            if (idUsuario <= 0) return RedirectToAction("Index", "Home");

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

