using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Bilito.Backoffice;
using Bilito.Backoffice.Api;
using Bilito.Backoffice.Api.Authentication;
using Bilito.Backoffice.Api.Users;
using Bilito.Backoffice.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddAuthorizationCore();

var apiBaseUrl = builder.Configuration["BilitoApi:BaseUrl"]
    ?? throw new InvalidOperationException("BilitoApi:BaseUrl is not configured.");

builder.Services.AddScoped<BackofficeAuthenticationState>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<BackofficeAuthenticationState>());
builder.Services.AddScoped<AccessTokenHandler>();
builder.Services.AddScoped<ApiRequestLog>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AccessTokenHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute) };
});
builder.Services.AddScoped<AuthenticationApiClient>();
builder.Services.AddScoped<UsersApiClient>();
builder.Services.AddScoped<SystemApiClient>();

await builder.Build().RunAsync();
