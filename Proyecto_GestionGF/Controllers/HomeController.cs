using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Proyecto_GestionGF.Models;
using System.Data;
using System.Diagnostics;

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


        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Index(Usuario usuario)
        {
            try
            {
                using (var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]))
                {
                    var parametros = new DynamicParameters();
                    parametros.Add("@CorreoElectronico", usuario.CorreoElectronico);
                    parametros.Add("@Password", usuario.Password);

                    var resultado = context.QueryFirstOrDefault<Usuario>(
                        "InicioSesion",
                        parametros,
                        commandType: CommandType.StoredProcedure
                    );

                    if (resultado == null)
                    {
                        TempData["Error"] = "Usuario o contraseña incorrectos.";
                        return View();
                    }

                    HttpContext.Session.SetInt32("IdUsuario", resultado.IdUsuario);
                    HttpContext.Session.SetString("Nombre", resultado.Nombre ?? string.Empty);
                    HttpContext.Session.SetInt32("IdRol", resultado.IdRol);
                    HttpContext.Session.SetString("Correo", resultado.CorreoElectronico ?? string.Empty);
                    HttpContext.Session.SetString("Perfil", resultado.NombreRol ?? string.Empty);

                    // Ruteo por rol:
                    if (resultado.IdRol == 1 || resultado.IdRol == 2)
                    {

                        return RedirectToAction("Main");
                    }
                    else
                    {
                        return RedirectToAction("MainUsuario");
                    }
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Ocurrió un error al iniciar sesión.";
                return View();
            }
        }


        [HttpGet]
        public IActionResult Main()
        {
            var rol = HttpContext.Session.GetInt32("IdRol");
            if (rol != 1 && rol != 2) return RedirectToAction("MainUsuario");

            ViewBag.Nombre = HttpContext.Session.GetString("Nombre");
            return View(); // Views/Home/Main.cshtml
        }

        [HttpGet]
        public IActionResult MainUsuario()
        {
            ViewBag.Nombre = HttpContext.Session.GetString("Nombre");
            return View(); // Views/Home/MainUsuario.cshtml
        }
    }
}
