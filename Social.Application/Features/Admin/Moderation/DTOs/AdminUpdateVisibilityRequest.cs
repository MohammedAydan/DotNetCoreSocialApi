namespace Social.Application.Features.Admin.Moderation.DTOs
{
    public class AdminUpdateVisibilityRequest
    {
        public string Visibility { get; set; } = "public";
        public string Reason { get; set; } = "Visibility updated by administration";
    }
}
