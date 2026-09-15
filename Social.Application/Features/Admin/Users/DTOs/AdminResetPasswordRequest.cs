namespace Social.Application.Features.Admin.Users.DTOs
{
    public class AdminResetPasswordRequest
    {
        public string? NewPassword { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
