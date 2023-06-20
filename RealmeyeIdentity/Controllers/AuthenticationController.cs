using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using RealmeyeIdentity.Authentication;
using RealmeyeIdentity.Filters;
using RealmeyeIdentity.Models;

namespace RealmeyeIdentity.Controllers
{
    public class AuthenticationController : Controller
    {
        private const string IdTokenQueryParam = "idToken";
        private const string SessionCookieName = "registration_session";

        private readonly IAuthenticationService _service;

        public AuthenticationController(IAuthenticationService service)
        {
            _service = service;
        }

        [HttpGet]
        [RedirectUriFilter]
        public IActionResult Login(
            string redirectUri,
            bool changePasswordSuccess = false)
        {
            if (string.IsNullOrEmpty(redirectUri))
            {
                return View("Error", new ErrorModel { Message = "redirect uri null" });
            }
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
                    ModelUtils.AddLoginError(ModelState, error);
                    return View(model);

                default:
                    throw new NotSupportedException();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Register(string redirectUri, bool restore = false)
        {
            RegistrationSession session = await _service.StartRegistration();
            SetRegistrationSessionId(session);
            RegisterModel model = new()
            {
                Code = session.Code,
                Restore = restore,
            };
            ViewData["RedirectUri"] = redirectUri;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(
            [FromForm] RegisterModel model,
            [FromQuery] string redirectUri,
            [FromQuery] bool restore)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!TryGetRegistrationSessionId(out string sessionId))
            {
                return View(model);
            }

            RegisterResult result = await _service.Register(
                sessionId,
                model.Name,
                model.Password,
                restore);

            switch (result)
            {
                case RegisterResult.Ok ok:
                    string uri = QueryHelpers.AddQueryString(redirectUri, IdTokenQueryParam, ok.IdToken);
                    return Redirect(uri);

                case RegisterResult.Error error:
                    ModelUtils.AddRegisterError(ModelState, model, error);
                    return View(model);

                default:
                    throw new NotSupportedException();
            }
        }

        [HttpGet]
        public IActionResult ChangePassword(string redirectUri)
        {
            ViewData["RedirectUri"] = redirectUri;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(
            [FromForm] ChangePasswordModel model,
            [FromQuery] string redirectUri)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ChangePasswordResult result = await _service.ChangePassword(
                model.Name,
                model.OldPassword,
                model.NewPassword);

            switch (result)
            {
                case ChangePasswordResult.Ok ok:
                    return RedirectToAction(nameof(Login));

                case ChangePasswordResult.Error error:
                    ModelUtils.AddChangePasswordError(ModelState, error);
                    return View(model);

                default:
                    throw new NotSupportedException();
            }
        }

        private bool TryGetRegistrationSessionId(out string sessionId)
        {
            sessionId = "";
            if (!Request.Cookies.TryGetValue(SessionCookieName, out string? cookieSessionId)
                || cookieSessionId == null)
            {
                return false;
            }
            sessionId = cookieSessionId;
            return true;
        }

        private void SetRegistrationSessionId(RegistrationSession session)
        {
            Response.Cookies.Append(SessionCookieName, session.Id, new()
            {
                HttpOnly = true,
                IsEssential = true,
                Expires = session.ExpiresAt,
            });
        }
    }
}
