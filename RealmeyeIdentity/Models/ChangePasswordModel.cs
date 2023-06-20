using System.ComponentModel.DataAnnotations;

namespace RealmeyeIdentity.Models
{
    public class ChangePasswordModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string OldPassword { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }
}
