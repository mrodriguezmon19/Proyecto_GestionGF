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



        // Se realiza el inicio de sesión y la validación de credenciales y rol
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


        // Main Administrador
        public IActionResult Main(DateTime? desde, DateTime? hasta, int? estado)
        {
            var rol = HttpContext.Session.GetInt32("IdRol");
            if (rol != 1 && rol != 2)
                return RedirectToAction("MainUsuario");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var vm = new AdminDashboardModel
            {
                Nombre = HttpContext.Session.GetString("Nombre") ?? "Administrador"
            };

            vm.Solicitudes = cn.Query<AdminSolicitudRow>(
                "SolicitudHistorialAdmin_Filtro",
                new
                {
                    Desde = desde,
                    Hasta = hasta,
                    Estado = estado
                },
                commandType: CommandType.StoredProcedure
            ).ToList();

            return View(vm);
        }

        //Main Usuario
        [HttpGet]
        public IActionResult MainUsuario()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            if (idUsuario <= 0) return RedirectToAction("Index");

            using var cn = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var vm = new UsuarioDashboardModel
            {
                Nombre = HttpContext.Session.GetString("Nombre") ?? "Usuario"
            };

            vm.UltimasSolicitudes = cn.Query<UsuarioSolicitudRow>(
                "Usuario_SolicitudesRecientes",
                new { IdUsuario = idUsuario, Top = 10 },
                commandType: CommandType.StoredProcedure
            ).ToList();

            return View(vm); // Views/Home/MainUsuario.cshtml
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
                ws.Cell(fila, 2).Value = item.NombreUsuario;
                ws.Cell(fila, 3).Value = item.NombrePermiso;
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

        // ================= EXPORTAR PDF =================

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


    }
}

