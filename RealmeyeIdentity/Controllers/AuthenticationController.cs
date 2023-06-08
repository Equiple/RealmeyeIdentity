using Microsoft.AspNetCore.Mvc;

namespace RealmeyeIdentity.Controllers
{
    public class AuthenticationController : Controller
    {
        public IActionResult Index(string callbackUrl)
        {
            return View();
        }
    }
}
