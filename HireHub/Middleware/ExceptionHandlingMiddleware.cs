using System.Net;
using System.Text.Json;
using HireHub.API.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HireHub.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                context.Response.ContentType = "application/json";

                var result = ex switch
                {
                    DuplicateEmailException dex => (HttpStatusCode.Conflict, (object)new { error = dex.Message }),
                    ValidationException vex => (HttpStatusCode.BadRequest, (object)new { error = vex.Message, details = vex.Errors }),
                    NotFoundException nfe => (HttpStatusCode.NotFound, (object)new { error = nfe.Message }),
                    UnauthorizedAccessException _ => (HttpStatusCode.Unauthorized, (object)new { error = "Unauthorized" }),
                    _ => (HttpStatusCode.InternalServerError, (object)new { error = "An unexpected error occurred." })
                };

                var statusCode = result.Item1;
                var payload = result.Item2;


                if (_env.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError)
                {
                    payload = new
                    {
                        error = ex.Message,
                        details = ex.StackTrace
                    };
                }

                context.Response.StatusCode = (int)statusCode;
                var json = JsonSerializer.Serialize(payload);
                await context.Response.WriteAsync(json);
            }
        }
    }
}
