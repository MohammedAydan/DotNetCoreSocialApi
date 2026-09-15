using Microsoft.Extensions.DependencyInjection;
using Social.Admin.Web.Services;

namespace Social.Admin.Web
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAdminWebUI(this IServiceCollection services)
        {
            services.AddScoped<IAdminDashboardService, AdminDashboardService>();
            return services;
        }
    }
}
