namespace Social.Application.Features.Admin.Users.DTOs
{
    public class AdminBanUserRequest
    {
        public string Reason { get; set; } = string.Empty;
        public int? DurationDays { get; set; }
    }
}
