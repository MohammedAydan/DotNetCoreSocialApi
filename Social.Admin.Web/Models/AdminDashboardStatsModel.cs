using System;

namespace Social.Admin.Web.Models
{
    public class AdminDashboardStatsModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers24h { get; set; }
        public int TotalPosts { get; set; }
        public int TotalComments { get; set; }
        public int TotalLikes { get; set; }
        public double MemoryWorkingSetMb { get; set; }
        public bool IsRedisConnected { get; set; }
        public bool IsMemoryFallbackActive { get; set; }
        public string Environment { get; set; } = "Production";
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}
