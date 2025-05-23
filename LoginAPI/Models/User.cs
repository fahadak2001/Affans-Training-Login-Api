using System.ComponentModel.DataAnnotations;

namespace LoginAPI.Models
{
    public class User
    {
        [Key]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Value is required.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Value is required.")]
        public string Password { get; set; }

        public string? Role { get; set; }
    }
}
