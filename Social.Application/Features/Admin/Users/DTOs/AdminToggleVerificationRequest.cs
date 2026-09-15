namespace Social.Application.Features.Admin.Users.DTOs
{
    public class AdminToggleVerificationRequest
    {
        public bool IsVerified { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
