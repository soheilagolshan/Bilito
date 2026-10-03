using Bilito.Identity.Application.Abstractions;
using Bilito.Identity.Application.Authentication;
using Bilito.Identity.Application.Configuration;
using Bilito.Identity.Application.Users;
using Bilito.Identity.Infrastructure.Authentication;
using Bilito.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bilito.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("ConnectionStrings:Database is not configured.");

        services.AddDbContext<IdentityDbContext>(options => options.UseSqlServer(connectionString));
        services.Configure<OtpOptions>(configuration.GetSection("Otp"));
        services.Configure<JwtOptions>(configuration.GetSection("Authentication:Jwt"));
        services.Configure<RefreshTokenOptions>(configuration.GetSection("Authentication:RefreshToken"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<OtpOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<RefreshTokenOptions>>().Value);
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IIdentityStore, IdentityStore>();
        services.AddScoped<IdentityAuthenticationService>();
        services.AddScoped<CurrentUserService>();
        services.AddSingleton<IMobileNumberNormalizer, MobileNumberNormalizer>();
        services.AddSingleton<IOtpCodeGenerator, SecureOtpCodeGenerator>();
        services.AddSingleton<IOtpCodeHasher, Sha256OtpCodeHasher>();
        services.AddSingleton<IOtpDelivery, DevelopmentOtpDelivery>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddSingleton<IRefreshTokenGenerator, SecureRefreshTokenGenerator>();

        return services;
    }
}
