using System.Security.Claims;
using Bilito.Identity.Application.Abstractions;
using Bilito.Identity.Application.Users;
using Bilito.Identity.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bilito.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users")]
public sealed class UsersController(CurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await currentUserService.GetAsync(userId, cancellationToken));
        }
        catch (IdentityUnauthorizedException exception)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Authentication failed", detail: exception.Message);
        }
    }

    [HttpPut("me")]
    public async Task<ActionResult<CurrentUserResponse>> UpdateCurrentUser(
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await currentUserService.UpdateAsync(userId, request, cancellationToken));
        }
        catch (IdentityValidationException exception)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid profile", detail: exception.Message);
        }
        catch (IdentityUnauthorizedException exception)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Authentication failed", detail: exception.Message);
        }
    }
}
