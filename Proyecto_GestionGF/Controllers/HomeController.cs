using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Proyecto_GestionGF.Models;

namespace Proyecto_GestionGF.Controllers
{
    public class HomeController : Controller
    {
            
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        public HomeController(IConfiguration configuration, IHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }


        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult InicioSesion()
        {
            return View();
        }

        [HttpPost]
        public IActionResult InicioSesion(Usuario usuario)
        {
            using (var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]))
            {
                var parametros = new DynamicParameters();
                parametros.Add("@CorreoElectronico", usuario.CorreoElectronico);
                parametros.Add("@Contrasenna", usuario.Password);
                var resultado = context.QueryFirstOrDefault<Usuario>("InicioSesion", parametros);

                if (resultado != null)
                {
                    return View("Main");
                }

                return NotFound();
            }
        }
    }
}
