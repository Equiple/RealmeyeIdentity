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
        [WithRedirectUri]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [WithRedirectUri]
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
        [WithRedirectUri]
        public async Task<IActionResult> Register(bool restore = false)
        {
            RegistrationSession? session = null;
            if (TryGetRegistrationSessionId(out string sessionId))
            {
                session = await _service.GetRegistrationSession(sessionId);
            }
            if (session == null)
            {
                session = await _service.StartRegistration();
                SetRegistrationSessionId(session);
            }
            RegisterModel model = new()
            {
                Code = session.Code,
                Restore = restore,
            };
            return View(model);
        }

        [HttpPost]
        [WithRedirectUri]
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
                model.SessionExpired = true;
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
        [WithRedirectUri]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [WithRedirectUri]
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
