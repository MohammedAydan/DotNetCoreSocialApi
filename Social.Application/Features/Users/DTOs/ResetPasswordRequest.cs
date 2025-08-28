using System.ComponentModel.DataAnnotations;

namespace Social.Application.Features.Users.DTOs
{
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        //[EmailAddress(ErrorMessage = "Invalid email address format.")]
        //[StringLength(100, ErrorMessage = "Email must not exceed 100 characters.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Reset token is required.")]
        //[StringLength(200, ErrorMessage = "Reset token must not exceed 200 characters.")]
        public string Token { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Confirm password is required.")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}