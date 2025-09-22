using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HireHub.API.Tests.Integration
{
    // Simple test authentication handler that reads two headers:
    // - X-TEST-USERID : a GUID string for the NameIdentifier claim
    // - X-TEST-ROLE   : the role claim (Employer|JobSeeker|Admin)
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string TestScheme = "Test";
        public const string HeaderUserId = "X-TEST-USERID";
        public const string HeaderRole = "X-TEST-ROLE";

        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
                               ILoggerFactory logger,
                               UrlEncoder encoder,
                               ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // If test headers are not present, don't authenticate (so the endpoint sees anonymous)
            if (!Request.Headers.ContainsKey(HeaderUserId) || !Request.Headers.ContainsKey(HeaderRole))
                return Task.FromResult(AuthenticateResult.NoResult());

            var userIdValue = Request.Headers[HeaderUserId].ToString();
            var roleValue = Request.Headers[HeaderRole].ToString();

            if (!Guid.TryParse(userIdValue, out var userGuid))
                return Task.FromResult(AuthenticateResult.Fail("Invalid test user id"));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userIdValue),
                new Claim(ClaimTypes.Role, roleValue),
            };

            var identity = new ClaimsIdentity(claims, TestScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, TestScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
