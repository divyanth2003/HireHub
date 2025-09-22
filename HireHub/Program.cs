using HireHub.API.Mappings;
using HireHub.API.Middleware;
using HireHub.API.Repositories.Implementations;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Services;
using HireHub.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// JWT config
var jwtConfig = builder.Configuration.GetSection("Jwt");
var key = jwtConfig["Key"] ?? throw new InvalidOperationException("Jwt:Key missing in configuration");

// Authentication (JWT)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtConfig["Issuer"],

        ValidateAudience = true,
        ValidAudience = jwtConfig["Audience"],

        ValidateLifetime = true,

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),

        ClockSkew = TimeSpan.Zero,

        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };
});

builder.Services.AddAuthorization();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement{
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme{
                Reference = new Microsoft.OpenApi.Models.OpenApiReference {
                    Id = "Bearer", Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme
                }
            },
            new string[]{}
        }
    });
});

// Database
builder.Services.AddDbContext<HireHubContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HireHub")));

// CORS - development friendly policy (replace origin with your frontend in prod)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCorsPolicy", policy =>
    {
        // In development allow your front-end origin(s). Example: React on localhost:3000
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    // Optional permissive for quick local testing (NOT for production)
    // options.AddPolicy("AllowAllLocal", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// Register repositories & services (kept your registrations)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();

builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
builder.Services.AddScoped<EmployerService>();

builder.Services.AddScoped<IJobSeekerRepository, JobSeekerRepository>();
builder.Services.AddScoped<JobSeekerService>();

builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<JobService>();

builder.Services.AddScoped<IResumeRepository, ResumeRepository>();
builder.Services.AddScoped<ResumeService>();

builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<ApplicationService>();

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<NotificationService>();

// Email service (you already have the simple logger implementation)
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Logging.ClearProviders();
builder.Logging.AddLog4Net("log4net.config");

builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();

// HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();

// IMPORTANT: enable CORS before Authorization so browser preflight/auth flows work
app.UseCors("DevCorsPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();

// Make Program public so integration tests using WebApplicationFactory<Program> can access it.
public partial class Program { }
