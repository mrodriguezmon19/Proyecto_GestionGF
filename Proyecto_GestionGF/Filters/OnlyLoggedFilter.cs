using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Proyecto_GestionGF.Filters
{
    public class OnlyLoggedFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var idUsuario = context.HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null || idUsuario <= 0)
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
