using Microsoft.AspNetCore.Mvc.ModelBinding;
using RealmeyeIdentity.Authentication;
using RealmeyeIdentity.Models;

namespace RealmeyeIdentity.Controllers
{
    public static class ModelUtils
    {
        public static void AddLoginError(
            ModelStateDictionary modelState,
            LoginResult.Error error)
        {
            switch (error.Type)
            {
                case LoginErrorType.NotFound:
                    modelState.AddModelError(nameof(LoginModel.Name), "Name not found");
                    break;
                case LoginErrorType.IncorrectPassword:
                    modelState.AddModelError(nameof(LoginModel.Password), "Incorrect password");
                    break;
            }
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
                        "Code not found in realmeye profile, paste code and/or try again");
                    break;
                case RegisterErrorType.SessionExpired:
                    model.SessionExpired = true;
                    break;
                case RegisterErrorType.AlreadyExists:
                    model.AlreadyExists = true;
                    break;
                case RegisterErrorType.RestoreNotFound:
                    modelState.AddModelError(nameof(RegisterModel.Name), "Name not found");
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
                    modelState.AddModelError(nameof(ChangePasswordModel.Name), "Name not found");
                    break;
                case ChangePasswordErrorType.IncorrectPassword:
                    modelState.AddModelError(nameof(ChangePasswordModel.OldPassword), "Incorrect password");
                    break;
            }
        }
    }
}
