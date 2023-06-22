using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using RealmeyeIdentity.Models;

namespace RealmeyeIdentity.Filters
{
    public class WithRedirectUri : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.Controller is not Controller controller)
            {
                return;
            }

            if (!TryGetRedirectUri(context.HttpContext, out _))
            {
                controller.ViewData.Model = new ErrorModel
                {
                    Message = "Redirect URI is null",
                };
                context.Result = new ViewResult
                {
                    ViewName = "Error",
                    ViewData = controller.ViewData,
                    TempData = controller.TempData,
                };
            }
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Controller is not Controller controller
                || !TryGetRedirectUri(context.HttpContext, out string redirectUri))
            {
                return;
            }

            controller.ViewData["RedirectUri"] = redirectUri;
        }

        private static bool TryGetRedirectUri(HttpContext context, out string redirectUri)
        {
            redirectUri = "";
            if (!context.Request.Query.TryGetValue("redirectUri", out StringValues redirUri)
                || StringValues.IsNullOrEmpty(redirUri))
            {
                return false;
            }
            redirectUri = redirUri;
            return true;
        }
    }
}
