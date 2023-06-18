using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using RealmeyeIdentity.Authentication;
using RealmeyeIdentity.Models;

namespace RealmeyeIdentity.Controllers
{
    public class AuthenticationController : Controller
    {
        private const string IdTokenQueryParam = "idToken";
        private const string CodeSessionKey = "code";

        private readonly IAuthenticationService _service;

        public AuthenticationController(IAuthenticationService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult Login(string redirectUri)
        {
            ViewData["RedirectUri"] = redirectUri;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(
            [FromForm] LoginModel model,
            [FromQuery] string redirectUri)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            LoginResult result = await _service.Login(model.Name, model.Password);

            switch (result)
            {
                case LoginResult.Ok ok:
                    string uri = QueryHelpers.AddQueryString(redirectUri, IdTokenQueryParam, ok.IdToken);
                    return Redirect(uri);

                case LoginResult.Error error:
                    return View(model);

                default:
                    throw new NotSupportedException();
            }
        }

        [HttpGet]
        public IActionResult Register(
            string redirectUri,
            [FromServices] ICodeGenerator codeGenerator)
        {
            if (!TryGetSessionCode(out byte[] codeBytes))
            {
                codeBytes = codeGenerator.GenerateCode();
                SetSessionCode(codeBytes);
            }
            string code = GetCodeString(codeBytes);
            RegisterModel model = new()
            {
                Code = code,
            };
            ViewData["RedirectUri"] = redirectUri;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(
            [FromForm] RegisterModel model,
            [FromQuery] string redirectUri)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!TryGetSessionCode(out byte[] codeBytes))
            {
                return View();
            }

            string code = GetCodeString(codeBytes);
            RegisterResult result = await _service.Register(model.Name, model.Password, code);

            switch (result)
            {
                case RegisterResult.Ok ok:
                    string uri = QueryHelpers.AddQueryString(redirectUri, IdTokenQueryParam, ok.IdToken);
                    return Redirect(uri);

                case RegisterResult.Error error:
                    return View(model);

                default:
                    throw new NotSupportedException();
            }
        }

        private bool TryGetSessionCode(out byte[] code)
        {
            if (!HttpContext.Session.TryGetValue(CodeSessionKey, out byte[]? sessionCode)
                || sessionCode == null)
            {
                code = Array.Empty<byte>();
                return false;
            }
            code = sessionCode;
            return true;
        }

        private void SetSessionCode(byte[] code)
        {
            HttpContext.Session.Set(CodeSessionKey, code);
        }

        private static string GetCodeString(byte[] codeBytes)
        {
            return Convert.ToBase64String(codeBytes);
        }
    }
}
