using System.ComponentModel.DataAnnotations;

namespace RealmeyeIdentity.Models
{
    public class RegisterModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Password { get; set; }

        public string Code { get; set; }
    }
}
