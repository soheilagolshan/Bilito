using Bilito.Identity.Application.Abstractions;
using Bilito.Identity.Application.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Bilito.Identity.Infrastructure.Authentication;

public sealed class DevelopmentOtpDelivery(
    IHostEnvironment environment,
    IOptions<OtpOptions> options) : IOtpDelivery
{
    public Task<string?> DeliverAsync(string mobile, string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!environment.IsDevelopment())
        {
            throw new IdentityDeliveryUnavailableException("No production OTP delivery provider is configured.");
        }

        var expose = environment.IsDevelopment() && options.Value.ExposeDevelopmentOtp;
        return Task.FromResult<string?>(expose ? code : null);
    }
}
