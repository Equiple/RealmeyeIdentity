using System.ComponentModel.DataAnnotations;

namespace RealmeyeIdentity.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Name is required")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare(nameof(Password), ErrorMessage = "Passwords don't match")]
        public string? PasswordConfirmation { get; set; }

        public string Code { get; set; }

        public int CodeExpiresInSeconds { get; set; }

        public bool Restore { get; set; }

        public List<string> PasswordErrors { get; set; } = new();

        public bool AlreadyExists { get; set; }
    }
}
