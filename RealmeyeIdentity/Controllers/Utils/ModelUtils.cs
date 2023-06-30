using Microsoft.AspNetCore.Mvc.ModelBinding;
using RealmeyeIdentity.Authentication;
using RealmeyeIdentity.Models;
using System.Text.RegularExpressions;

namespace RealmeyeIdentity.Controllers
{
    public static class ModelUtils
    {
        public static void AddLoginError(
            ModelStateDictionary modelState,
            LoginModel model,
            LoginResult.Error error)
        {
            switch (error.Type)
            {
                case LoginErrorType.NotFound:
                    model.NotFound = true;
                    break;
                case LoginErrorType.IncorrectPassword:
                    modelState.AddModelError(nameof(LoginModel.Password), "Incorrect password");
                    break;
            }
        }

        public static bool ValidateRegisterPassword(RegisterModel model)
        {
            if (model.Password != null)
            {
                if (model.Password.Length < 8)
                {
                    model.PasswordErrors.Add("Password shorter than 8 characters");
                }
                if (!Regex.IsMatch(model.Password, "[a-zA-Z]")
                    || !Regex.IsMatch(model.Password, "[0-9]"))
                {
                    model.PasswordErrors.Add("Password must contain both letters and numbers");
                }
            }
            return model.PasswordErrors.Count == 0;
        }

        public static int GetExpiresInSeconds(this RegistrationSession session)
        {
            return (int)session.ExpiresAt
                .Subtract(DateTimeOffset.UtcNow)
                .TotalSeconds;
        }

        public static void AddRegisterError(
            ModelStateDictionary modelState,
            RegisterModel model,
            RegisterResult.Error error)
        {
            switch (error.Type)
            {
                case RegisterErrorType.IncorrectCode:
                    modelState.AddModelError(
                        nameof(RegisterModel.Code),
                        "Code is not found in RealmEye profile");
                    break;
                case RegisterErrorType.AlreadyExists:
                    model.AlreadyExists = true;
                    break;
                case RegisterErrorType.RestoreNotFound:
                    modelState.AddModelError(nameof(RegisterModel.Name), "Name is not found");
                    break;
            }
        }

        public static void AddChangePasswordError(
            ModelStateDictionary modelState,
            ChangePasswordResult.Error error)
        {
            switch (error.Type)
            {
                case ChangePasswordErrorType.NotFound:
                    modelState.AddModelError(nameof(ChangePasswordModel.Name), "Name is not found");
                    break;
                case ChangePasswordErrorType.IncorrectPassword:
                    modelState.AddModelError(nameof(ChangePasswordModel.OldPassword), "Incorrect password");
                    break;
            }
        }
    }
}
