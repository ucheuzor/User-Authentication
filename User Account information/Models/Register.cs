using System.ComponentModel.DataAnnotations;

namespace User_Account_information.Models
{
    public class Register
    {
        [Required(ErrorMessage = "User Name is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        [EmailAddress]
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }

        public string Role { get; set; }
    }
}
