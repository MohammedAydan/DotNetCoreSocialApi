namespace Social.Application.Features.Admin.Analytics.DTOs
{
    public class PlatformOverviewMetricsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers24h { get; set; }
        public int TotalPosts { get; set; }
        public int TotalComments { get; set; }
        public int TotalLikes { get; set; }
    }
}
