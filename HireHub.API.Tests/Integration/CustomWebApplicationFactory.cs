using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HireHub.Data;
using HireHub.API.Models;

namespace HireHub.API.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Guid SeedAdminUserId { get; } = Guid.NewGuid();
        public string SeedAdminEmail { get; } = "admin@hirehub.test";

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTest");
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // ---------- Robustly remove existing DbContext/DbContextOptions registrations ----------
                // We'll remove descriptors that reference HireHubContext or DbContextOptions< HireHubContext >
                var toRemove = services.Where(d =>
                    // direct service type matches
                    d.ServiceType == typeof(HireHubContext)
                    || (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)
                        && d.ServiceType.GetGenericArguments()[0] == typeof(HireHubContext))
                    // implementation type or instance naming
                    || (d.ImplementationType != null && (d.ImplementationType == typeof(HireHubContext) || d.ImplementationType.Name.Contains("HireHubContext")))
                    || (d.ImplementationInstance != null && d.ImplementationInstance.GetType().Name.Contains("HireHubContext"))
                    // factory that constructs HireHubContext (best-effort: string match)
                    || (d.ImplementationFactory != null && d.ImplementationFactory.Method.ReturnType.Name.Contains("HireHubContext"))
                ).ToList();

                foreach (var d in toRemove)
                    services.Remove(d);

                // As an extra precaution: remove any registration that mentions SqlServer EF provider options
                var maybeSql = services.Where(d =>
                    (d.ServiceType?.FullName?.Contains("DbContextOptions") ?? false)
                    || (d.ImplementationType?.FullName?.Contains("DbContextOptions") ?? false)
                ).ToList();
                // Don't remove everything blindly; only remove ones that refer to HireHub or DbContextOptions<>
                foreach (var d in maybeSql)
                {
                    if (!services.Contains(d)) continue;
                    // still safe to remove if it references HireHub by name in the descriptor strings
                    if ((d.ServiceType?.Name?.Contains("DbContext") ?? false) ||
                        (d.ImplementationType?.Name?.Contains("DbContext") ?? false))
                    {
                        services.Remove(d);
                    }
                }

                // ---------- Register InMemory DB ----------
                services.AddDbContext<HireHubContext>(options =>
                {
                    options.UseInMemoryDatabase("HireHubIntegrationTestDb");
                });

                // ---------- Replace authentication with test scheme ----------
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.TestScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.TestScheme;
                })
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.TestScheme, options => { });

                // ---------- Build service provider and seed ----------
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<HireHubContext>();

                // Ensure clean DB and seed basic data
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                if (!db.Users.Any(u => u.Email == SeedAdminEmail))
                {
                    var admin = new User
                    {
                        UserId = SeedAdminUserId,
                        FullName = "Integration Admin",
                        Email = SeedAdminEmail,
                        PasswordHash = "hashed-password",
                        Role = "Admin",
                        Dateofbirth = DateTime.UtcNow.AddYears(-30),
                        Gender = "Other",
                        Address = "Test Address",
                        CreatedAt = DateTime.UtcNow
                    };
                    db.Users.Add(admin);
                    db.SaveChanges();
                }
            });
        }
    }
}
