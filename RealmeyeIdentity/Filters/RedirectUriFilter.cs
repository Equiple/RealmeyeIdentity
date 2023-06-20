using Microsoft.AspNetCore.Mvc.Filters;

namespace RealmeyeIdentity.Filters
{
    public class RedirectUriFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
        }
    }
}
