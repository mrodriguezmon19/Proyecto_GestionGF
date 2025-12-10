using Dapper;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;

namespace Proyecto_GestionGF.Filters
{
    public class AuditoriaFiltro : IActionFilter
    {
        private readonly IConfiguration _cfg;
        private readonly IHttpContextAccessor _http;

        public AuditoriaFiltro(IConfiguration cfg, IHttpContextAccessor http)
        {
            _cfg = cfg; _http = http;
        }

        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            try
            {
                var http = _http.HttpContext!;
                int? idUsuario = http.Session.GetInt32("IdUsuario");
                int? idRol = http.Session.GetInt32("IdRol"); // tu rol (1=Admin, 2=Jefe, etc.)
                int? idDepartamento = http.Session.GetInt32("IdDepartamento");    // si aún no lo manejan, quedará NULL

                // Accion por defecto: "Controller.Action"
                string accion = $"{context.RouteData.Values["controller"]}.{context.RouteData.Values["action"]}";

                // Si desde la acción quieres forzar un nombre específico:
                if (context.RouteData.Values.TryGetValue("AccionAuditoria", out var custom) && custom is string s && !string.IsNullOrWhiteSpace(s))
                    accion = s;

                string? estacionTrabajo = null; // lo dejamos NULL

                using var cn = new SqlConnection(_cfg["ConnectionStrings:BDConnection"]);
                cn.Execute("dbo.Auditoria_Insertar",
                    new
                    {
                        IdUsuario = idUsuario,
                        IdRol = idRol,
                        IdDepartamento = idDepartamento,
                        Accion = accion,
                        EstacionTrabajo = estacionTrabajo
                    },
                    commandType: System.Data.CommandType.StoredProcedure);
            }
            catch
            {
                // Nunca romper el flujo por falla de auditoría
            }
        }
    }
}
