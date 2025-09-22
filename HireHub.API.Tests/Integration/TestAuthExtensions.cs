using System;
using System.Net.Http;

namespace HireHub.API.Tests.Integration
{
    public static class TestAuthExtensions
    {
        /// <summary>
        /// Create an HttpClient that sends the test auth headers on every request.
        /// Defaults to the factory's seeded admin user id if userId is null.
        /// </summary>
        public static HttpClient CreateClientWithTestAuth(this CustomWebApplicationFactory factory, string role = "Admin", Guid? userId = null)
        {
            var client = factory.CreateClient();
            var uid = (userId ?? factory.SeedAdminUserId).ToString();
            client.DefaultRequestHeaders.Remove(TestAuthHandler.HeaderUserId);
            client.DefaultRequestHeaders.Remove(TestAuthHandler.HeaderRole);
            client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderUserId, uid);
            client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderRole, role);
            return client;
        }

        /// <summary>
        /// Attach test auth headers to an existing client (useful when you need non-default BaseAddress).
        /// </summary>
        public static void AttachTestAuth(this HttpClient client, Guid userId, string role = "JobSeeker")
        {
            client.DefaultRequestHeaders.Remove(TestAuthHandler.HeaderUserId);
            client.DefaultRequestHeaders.Remove(TestAuthHandler.HeaderRole);
            client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderUserId, userId.ToString());
            client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderRole, role);
        }
    }
}
