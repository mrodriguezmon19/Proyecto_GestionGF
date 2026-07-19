using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Proyecto_GestionGF.Filters;
using Proyecto_GestionGF.Models;

namespace Proyecto_GestionGF.Controllers
{
    [OnlyAdminFilter]
    public class AuditoriaController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuditoriaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private void CargarNotificaciones(SqlConnection cn)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;

            ViewBag.NotificacionesNoLeidas = cn.QueryFirstOrDefault<int>(
                "Notificacion_ContarNoLeidas",
                new { IdUsuario = idUsuario },
                commandType: CommandType.StoredProcedure
            );
        }

        [HttpGet]
        public IActionResult Index()
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var lista = cn.Query<AuditoriaModel>(
                "Auditoria_Listar",
                commandType: CommandType.StoredProcedure
            ).ToList();

            ViewBag.Nombre = HttpContext.Session.GetString("Nombre") ?? "Administrador";
            CargarNotificaciones(cn);

            return View(lista);
        }
    }
}
