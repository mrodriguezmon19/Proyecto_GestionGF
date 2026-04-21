using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Proyecto_GestionGF.Models;
using System.Data;
using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using Proyecto_GestionGF.Filters;

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

        private void CargarHeaderAdmin(SqlConnection cn)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;

            ViewBag.Nombre = HttpContext.Session.GetString("Nombre") ?? "Administrador";

            ViewBag.NotificacionesNoLeidas = cn.QueryFirstOrDefault<int>(
                "Notificacion_ContarNoLeidas",
                new { IdUsuario = idUsuario },
                commandType: CommandType.StoredProcedure
            );
        }

        private void CargarHeaderUsuario(SqlConnection cn)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;

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
            return View();
        }

        [HttpPost]
        public IActionResult Index(Usuario usuario)
        {
            try
            {
                using (var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]))
                {
                    var usuarioBD = context.QueryFirstOrDefault<Usuario>(
                        "SELECT * FROM Usuario WHERE CorreoElectronico = @correo",
                        new { correo = usuario.CorreoElectronico });

                    if (usuarioBD == null)
                    {
                        TempData["Error"] = "Usuario no encontrado";
                        return View();
                    }

                    if (usuarioBD.Bloqueado && usuarioBD.FechaBloqueo != null)
                    {
                        if (DateTime.Now >= usuarioBD.FechaBloqueo.Value.AddMinutes(5))
                        {
                            context.Execute(
                                @"UPDATE Usuario 
                                  SET Bloqueado = 0, IntentosFallidos = 0, FechaBloqueo = NULL 
                                  WHERE IdUsuario = @id",
                                new { id = usuarioBD.IdUsuario });

                            usuarioBD.Bloqueado = false;
                            usuarioBD.IntentosFallidos = 0;
                        }
                    }

                    if (usuarioBD.Bloqueado)
                    {
                        TempData["Error"] = "Cuenta bloqueada. Intente nuevamente en 5 minutos.";
                        return View();
                    }

                    if (usuarioBD.Password != usuario.Password)
                    {
                        int intentos = usuarioBD.IntentosFallidos + 1;
                        bool bloquear = intentos >= 5;

                        context.Execute(
                            @"UPDATE Usuario 
                              SET IntentosFallidos = @intentos,
                                  Bloqueado = @bloqueado,
                                  FechaBloqueo = CASE 
                                      WHEN @bloqueado = 1 THEN GETDATE() 
                                      ELSE FechaBloqueo 
                                  END
                              WHERE IdUsuario = @id",
                            new
                            {
                                intentos,
                                bloqueado = bloquear,
                                id = usuarioBD.IdUsuario
                            });

                        if (bloquear)
                        {
                            TempData["Error"] = "Cuenta bloqueada por 5 intentos fallidos";
                        }
                        else
                        {
                            TempData["Error"] = $"Credenciales incorrectas. Intento {intentos}/5";
                        }

                        return View();
                    }

                    context.Execute(
                        @"UPDATE Usuario 
                          SET IntentosFallidos = 0, Bloqueado = 0, FechaBloqueo = NULL 
                          WHERE IdUsuario = @id",
                        new { id = usuarioBD.IdUsuario });

                    HttpContext.Session.SetInt32("IdUsuario", usuarioBD.IdUsuario);
                    HttpContext.Session.SetString("Nombre", usuarioBD.Nombre ?? string.Empty);
                    HttpContext.Session.SetInt32("IdRol", usuarioBD.IdRol);
                    HttpContext.Session.SetString("Correo", usuarioBD.CorreoElectronico ?? string.Empty);
                    HttpContext.Session.SetString("Perfil", usuarioBD.NombreRol ?? string.Empty);

                    if (usuarioBD.IdRol == 1 || usuarioBD.IdRol == 2)
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

        [OnlyAdminFilter]
        public IActionResult Main(DateTime? desde, DateTime? hasta, int? estado)
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var vm = new AdminDashboardModel
            {
                Nombre = HttpContext.Session.GetString("Nombre") ?? "Administrador",
            };

            vm.Solicitudes = cn.Query<AdminSolicitudRow>(
                "SolicitudHistorialAdmin_Filtro",
                new
                {
                    Desde = desde,
                    Hasta = hasta,
                    Estado = estado,
                },
                commandType: CommandType.StoredProcedure
            ).ToList();

            vm.TotalPendientes = cn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM Solicitud WHERE Estado = 0"
            );

            vm.TotalAprobados = cn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM Solicitud WHERE Estado = 1"
            );

            vm.TotalRechazados = cn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM Solicitud WHERE Estado = 2"
            );

            vm.TotalProximos = cn.ExecuteScalar<int>(
                @"SELECT COUNT(*) 
                  FROM Solicitud 
                  WHERE FechaInicio BETWEEN CAST(GETDATE() AS DATE) 
                  AND DATEADD(DAY, 7, CAST(GETDATE() AS DATE))"
            );

            // Para el layout
            CargarHeaderAdmin(cn);

            return View(vm);
        }

        [OnlyUserFilter]
        [HttpGet]
        public IActionResult MainUsuario()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            if (idUsuario <= 0)
                return RedirectToAction("Index");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var vm = new UsuarioDashboardModel
            {
                Nombre = HttpContext.Session.GetString("Nombre") ?? "Usuario",
            };

            vm.UltimasSolicitudes = cn.Query<UsuarioSolicitudRow>(
                "Usuario_SolicitudesRecientes",
                new { IdUsuario = idUsuario, Top = 10 },
                commandType: CommandType.StoredProcedure
            ).ToList();

            vm.TotalPendientes = cn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM Solicitud WHERE Estado = 0 AND IdUsuario = @IdUsuario",
                new { IdUsuario = idUsuario }
            );

            vm.TotalAprobados = cn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM Solicitud WHERE Estado = 1 AND IdUsuario = @IdUsuario",
                new { IdUsuario = idUsuario }
            );

            vm.TotalRechazados = cn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM Solicitud WHERE Estado = 2 AND IdUsuario = @IdUsuario",
                new { IdUsuario = idUsuario }
            );

            vm.TotalProximos = cn.ExecuteScalar<int>(
                @"SELECT COUNT(*) 
                  FROM Solicitud 
                  WHERE IdUsuario = @IdUsuario
                  AND FechaInicio BETWEEN CAST(GETDATE() AS DATE) 
                  AND DATEADD(DAY, 7, CAST(GETDATE() AS DATE))",
                new { IdUsuario = idUsuario }
            );

            // Para el layout
            CargarHeaderUsuario(cn);

            return View(vm);
        }

        [HttpGet]
        public IActionResult ExportarExcel(DateTime? desde, DateTime? hasta, int? estado)
        {
            var rol = HttpContext.Session.GetInt32("IdRol");
            if (rol != 1 && rol != 2)
                return RedirectToAction("Index");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var lista = cn.Query<SolicitudRow>(
                "SolicitudHistorialAdmin_Filtro",
                new { Desde = desde, Hasta = hasta, Estado = estado },
                commandType: CommandType.StoredProcedure
            ).ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Reporte");

            ws.Cell(1, 1).Value = "ID";
            ws.Cell(1, 2).Value = "Usuario";
            ws.Cell(1, 3).Value = "Tipo";
            ws.Cell(1, 4).Value = "Inicio";
            ws.Cell(1, 5).Value = "Fin";
            ws.Cell(1, 6).Value = "Estado";

            int fila = 2;

            foreach (var item in lista)
            {
                ws.Cell(fila, 1).Value = item.IdSolicitud;
                ws.Cell(fila, 2).Value = item.NombreUsuario ?? "";
                ws.Cell(fila, 3).Value = item.NombrePermiso ?? "";
                ws.Cell(fila, 4).Value = item.FechaInicio.ToString("dd/MM/yyyy");
                ws.Cell(fila, 5).Value = item.FechaFinal.ToString("dd/MM/yyyy");
                ws.Cell(fila, 6).Value = item.Estado;
                fila++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ReporteSolicitudes.xlsx");
        }

        [HttpGet]
        public IActionResult ExportarPdf(DateTime? desde, DateTime? hasta, int? estado)
        {
            var rol = HttpContext.Session.GetInt32("IdRol");
            if (rol != 1 && rol != 2)
                return RedirectToAction("Index");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var lista = cn.Query<SolicitudRow>(
                "SolicitudHistorialAdmin_Filtro",
                new { Desde = desde, Hasta = hasta, Estado = estado },
                commandType: CommandType.StoredProcedure
            ).ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            document.Add(
                new Paragraph("Reporte de Solicitudes")
                    .SetFont(bold)
                    .SetFontSize(16)
            );

            document.Add(new Paragraph(" "));

            var table = new Table(6);

            table.AddHeaderCell(new Paragraph("ID").SetFont(bold));
            table.AddHeaderCell(new Paragraph("Usuario").SetFont(bold));
            table.AddHeaderCell(new Paragraph("Tipo").SetFont(bold));
            table.AddHeaderCell(new Paragraph("Inicio").SetFont(bold));
            table.AddHeaderCell(new Paragraph("Fin").SetFont(bold));
            table.AddHeaderCell(new Paragraph("Estado").SetFont(bold));

            foreach (var item in lista)
            {
                table.AddCell(new Paragraph(item.IdSolicitud.ToString()).SetFont(normal));
                table.AddCell(new Paragraph(item.NombreUsuario ?? "").SetFont(normal));
                table.AddCell(new Paragraph(item.NombrePermiso ?? "").SetFont(normal));
                table.AddCell(new Paragraph(item.FechaInicio.ToString("dd/MM/yyyy")).SetFont(normal));
                table.AddCell(new Paragraph(item.FechaFinal.ToString("dd/MM/yyyy")).SetFont(normal));
                table.AddCell(new Paragraph(item.Estado.ToString()).SetFont(normal));
            }

            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "ReporteSolicitudes.pdf");
        }

        [HttpGet]
        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult RecuperarPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RecuperarPassword(string correo)
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var usuario = cn.QueryFirstOrDefault<Usuario>(
                "SELECT * FROM Usuario WHERE CorreoElectronico = @correo",
                new { correo });

            if (usuario == null)
            {
                TempData["Error"] = "Correo no encontrado";
                return View();
            }

            var token = Guid.NewGuid().ToString();

            cn.Execute(@"INSERT INTO RecuperacionPassword 
                (Correo, Token, FechaExpiracion)
                VALUES (@correo, @token, DATEADD(MINUTE, 30, GETDATE()))",
                new { correo, token });

            var link = Url.Action("CambiarPassword", "Home", new { token }, Request.Scheme);

            ViewBag.Link = link;

            return View();
        }

        [HttpGet]
        public IActionResult CambiarPassword(string token)
        {
            return View(model: token);
        }

        [HttpPost]
        public IActionResult CambiarPassword(string token, string nuevaPassword)
        {
            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var registro = cn.QueryFirstOrDefault<dynamic>(
                @"SELECT * FROM RecuperacionPassword 
                  WHERE Token = @token 
                  AND Usado = 0 
                  AND FechaExpiracion > GETDATE()",
                new { token });

            if (registro == null)
            {
                TempData["Error"] = "Token inválido o expirado";
                return View(model: token);
            }

            var hash = nuevaPassword;

            cn.Execute(
                "UPDATE Usuario SET Password = @pass WHERE CorreoElectronico = @correo",
                new { pass = hash, correo = registro.Correo });

            cn.Execute(
                "UPDATE RecuperacionPassword SET Usado = 1 WHERE Token = @token",
                new { token });

            TempData["Ok"] = "Contraseña actualizada correctamente";

            return RedirectToAction("Index");
        }
    }
}


