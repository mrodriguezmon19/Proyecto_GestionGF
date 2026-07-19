using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Proyecto_GestionGF.Filters
{
    public class OnlyUserFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var rol = context.HttpContext.Session.GetInt32("IdRol");

            if (rol == null)
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            if (rol == 1 || rol == 2)
            {
                context.Result = new RedirectToActionResult("Main", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
