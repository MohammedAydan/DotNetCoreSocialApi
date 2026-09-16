using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Social.Core.Interfaces;

namespace Social.Tests.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public IUserRepository MockUserRepository { get; } = Substitute.For<IUserRepository>();
        public IPostRepository MockPostRepository { get; } = Substitute.For<IPostRepository>();
        public ICommentRepository MockCommentRepository { get; } = Substitute.For<ICommentRepository>();
        public IFollowRepository MockFollowRepository { get; } = Substitute.For<IFollowRepository>();
        public ILikeRepository MockLikeRepository { get; } = Substitute.For<ILikeRepository>();
        public INotificationRepository MockNotificationRepository { get; } = Substitute.For<INotificationRepository>();
        public IBlockUserRepository MockBlockUserRepository { get; } = Substitute.For<IBlockUserRepository>();
        public ITokenService MockTokenService { get; } = Substitute.For<ITokenService>();
        public ICacheService MockCacheService { get; } = Substitute.For<ICacheService>();
        public IAdminRepository AdminRepositoryInstance { get; } = new TestAdminRepository();
        public IAuditLogRepository AuditLogRepositoryInstance { get; } = new TestAuditLogRepository();
        public IPostReportRepository PostReportRepositoryInstance { get; } = new TestPostReportRepository();

        static CustomWebApplicationFactory()
        {
            Environment.SetEnvironmentVariable("CONNECTION_STRING", "Server=localhost;Database=social_test;Uid=root;Pwd=secret;");
            Environment.SetEnvironmentVariable("JWT_KEY", "SuperSecretKeyForIntegrationTestingAtLeast32BytesLong123456!");
            Environment.SetEnvironmentVariable("JWT_ISSUER", "SocialApiTestIssuer");
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", "SocialApiTestAudience");
            Environment.SetEnvironmentVariable("API_KEY", "TestApiKey");
            Environment.SetEnvironmentVariable("FRONTEND_URL", "http://localhost:3000");
        }

        public CustomWebApplicationFactory()
        {
            // Default mock behaviors
            MockTokenService.IsTokenBlacklistedAsync(Arg.Any<string>()).Returns(false);

            MockPostRepository.GetPostByIdAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var postId = callInfo.ArgAt<string>(0);
                    var post = AdminRepositoryInstance.GetPostByIdAsync(postId).GetAwaiter().GetResult();
                    if (post == null || post.IsDeleted)
                    {
                        return null!;
                    }
                    return post;
                });
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["IpRateLimiting:GeneralRules:0:Limit"] = "10000",
                    ["IpRateLimiting:GeneralRules:1:Limit"] = "10000",
                    ["IpRateLimiting:GeneralRules:2:Limit"] = "10000"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Replace repositories and services with test doubles
                ReplaceScoped(services, MockUserRepository);
                ReplaceScoped(services, MockPostRepository);
                ReplaceScoped(services, MockCommentRepository);
                ReplaceScoped(services, MockFollowRepository);
                ReplaceScoped(services, MockLikeRepository);
                ReplaceScoped(services, MockNotificationRepository);
                ReplaceScoped(services, MockBlockUserRepository);
                ReplaceScoped(services, MockTokenService);
                ReplaceSingleton(services, MockCacheService);
                ReplaceScoped(services, AdminRepositoryInstance);
                ReplaceScoped(services, AuditLogRepositoryInstance);
                ReplaceScoped<IPostReportRepository>(services, PostReportRepositoryInstance);

                // Configure test authentication
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "TestScheme";
                    options.DefaultChallengeScheme = "TestScheme";
                    options.DefaultScheme = "TestScheme";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });
            });
        }

        private static void ReplaceScoped<T>(IServiceCollection services, T instance) where T : class
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
            if (descriptor != null) services.Remove(descriptor);
            services.AddScoped(_ => instance);
        }

        private static void ReplaceSingleton<T>(IServiceCollection services, T instance) where T : class
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
            if (descriptor != null) services.Remove(descriptor);
            services.AddSingleton(_ => instance);
        }
    }
}
