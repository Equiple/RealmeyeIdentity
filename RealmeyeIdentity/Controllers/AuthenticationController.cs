using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using RealmeyeIdentity.Authentication;
using RealmeyeIdentity.Filters;
using RealmeyeIdentity.Models;

namespace RealmeyeIdentity.Controllers
{
    public class AuthenticationController : Controller
    {
        private const string AuthCodeQueryParam = "authCode";
        private const string RegistrationCookieName = "registration_session";

        private readonly IAuthenticationService _service;

        public AuthenticationController(IAuthenticationService service)
        {
            _service = service;
        }

        [HttpGet]
        [WithRedirectUri]
        public IActionResult Login(string? name = null)
        {
            LoginModel model = new() { Name = name };
            return View(model);
        }

        [HttpPost]
        [WithRedirectUri]
        public async Task<IActionResult> Login(
            [FromForm] LoginModel model,
            [FromQuery] string redirectUri)
        {
            if (!ModelState.IsValid
                || model.Name == null
                || model.Password == null)
            {
                return View(model);
            }

            LoginResult result = await _service.Login(model.Name, model.Password);

            switch (result)
            {
                case LoginResult.Ok ok:
                    string uri = QueryHelpers.AddQueryString(
                        redirectUri,
                        AuthCodeQueryParam,
                        ok.AuthCode);
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
        public async Task<IActionResult> Register(bool restore = false, string? name = null)
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
                Name = name,
                Code = session.Code,
                Restore = restore,
            };
            ModelUtils.SetCodeExpiration(model, session);
            return View(model);
        }

        [HttpPost]
        [WithRedirectUri]
        public async Task<IActionResult> Register(
            [FromForm] RegisterModel model,
            [FromQuery] string redirectUri,
            [FromQuery] bool restore)
        {
            RegistrationSession? session = null;
            if (TryGetRegistrationSessionId(out string sessionId))
            {
                session = await _service.GetRegistrationSession(sessionId);
            }

            if (session == null)
            {
                return RedirectToAction(nameof(Register), new
                {
                    redirectUri,
                    name = model.Name,
                });
            }

            bool passwordValid = ModelUtils.ValidateRegisterPassword(model);
            if (!ModelState.IsValid
                || !passwordValid
                || model.Name == null
                || model.Password == null)
            {
                ModelUtils.SetCodeExpiration(model, session);
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
                    RemoveRegistrationSessionId();
                    string uri = QueryHelpers.AddQueryString(
                        redirectUri,
                        AuthCodeQueryParam,
                        ok.AuthCode);
                    return Redirect(uri);

                case RegisterResult.Error error:
                    if (error.Type == RegisterErrorType.SessionExpired)
                    {
                        return RedirectToAction(nameof(Register), new
                        {
                            redirectUri,
                            name = model.Name,
                        });
                    }
                    ModelUtils.AddRegisterError(ModelState, model, error);
                    ModelUtils.SetCodeExpiration(model, session);
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
                    return RedirectToAction(nameof(Login), new
                    {
                        redirectUri,
                        name = model.Name,
                    });

                case ChangePasswordResult.Error error:
                    ModelUtils.AddChangePasswordError(ModelState, error);
                    return View(model);

                default:
                    throw new NotSupportedException();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetToken([FromBody] TokenRequest request)
        {
            string? idToken = await _service.CreateIdTokenAsync(request.AuthCode);
            if (idToken == null)
            {
                return Unauthorized();
            }
            return Ok(new TokenResponse { IdToken = idToken });
        }

        private bool TryGetRegistrationSessionId(out string sessionId)
        {
            sessionId = "";
            if (!Request.Cookies.TryGetValue(RegistrationCookieName, out string? cookieSessionId)
                || cookieSessionId == null)
            {
                return false;
            }
            sessionId = cookieSessionId;
            return true;
        }

        private void SetRegistrationSessionId(RegistrationSession session)
        {
            Response.Cookies.Append(RegistrationCookieName, session.Id, new()
            {
                HttpOnly = true,
                IsEssential = true,
                Expires = session.ExpiresAt,
            });
        }

        private void RemoveRegistrationSessionId()
        {
            Response.Cookies.Delete(RegistrationCookieName);
        }
    }
}
