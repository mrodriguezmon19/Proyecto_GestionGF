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

        [HttpGet]
        public IActionResult Index()
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var lista = cn.Query<AuditoriaModel>(
                "Auditoria_Listar",
                commandType: CommandType.StoredProcedure
            ).ToList();

            ViewBag.Nombre = HttpContext.Session.GetString("Nombre") ?? "Administrador";

            return View(lista);
        }
    }
}
