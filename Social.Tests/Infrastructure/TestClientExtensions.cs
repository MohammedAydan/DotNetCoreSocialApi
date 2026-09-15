using System.Net.Http;

namespace Social.Tests.Infrastructure
{
    public static class TestClientExtensions
    {
        public static HttpClient CreateAdminClient(this CustomWebApplicationFactory factory, string userId = "admin-user-id")
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
            return client;
        }

        public static HttpClient CreateModeratorClient(this CustomWebApplicationFactory factory, string userId = "moderator-user-id")
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "Moderator");
            client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
            return client;
        }

        public static HttpClient CreateUserClient(this CustomWebApplicationFactory factory, string userId = "regular-user-id")
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Role", "User");
            client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
            return client;
        }

        public static HttpClient CreateAnonymousClient(this CustomWebApplicationFactory factory)
        {
            var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Add("X-Anonymous", "true");
            return client;
        }
    }
}
