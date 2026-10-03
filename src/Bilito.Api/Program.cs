using Bilito.Identity.Application.Configuration;
using Bilito.Identity.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Never allow an empty JWT key to reach SymmetricSecurityKey.
var jwtConfiguration = builder.Configuration.GetSection("Authentication:Jwt");
if (string.IsNullOrWhiteSpace(jwtConfiguration["SigningKey"]))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Authentication:Jwt:SigningKey is missing. Configure it with user-secrets or JWT__SIGNINGKEY before starting the API.");
    }

    // Development-only fallback. A fresh key is generated when no local secret exists.
    builder.Configuration["Authentication:Jwt:SigningKey"] =
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddIdentityModule(builder.Configuration);

var jwtOptions = builder.Configuration.GetSection("Authentication:Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
    string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException(
        "Authentication:Jwt must contain Issuer, Audience, and a non-empty SigningKey.");
}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

const string developmentCorsPolicy = "DevelopmentCors";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options => options.AddPolicy(developmentCorsPolicy, policy =>
    policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors(developmentCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;
