using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SchemaForge.Api.Common;
using SchemaForge.Api.Extensions;
using SchemaForge.Api.Mapping;
using SchemaForge.Contracts.V1.Auth;

namespace SchemaForge.Api.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    // Stricter than the global default (Step 10 Phase 7) - these are the two endpoints a
    // credential-stuffing or account-enumeration attempt would actually hammer, unlike the rest
    // of the API which only an authenticated caller can reach at all.
    [HttpPost("register")]
    [EnableRateLimiting(ApiServiceCollectionExtensions.AuthRateLimitPolicy)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    [HttpPost("login")]
    [EnableRateLimiting(ApiServiceCollectionExtensions.AuthRateLimitPolicy)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    // Anonymous like Login itself - a caller with an expired/missing access token has nothing
    // else to authenticate with, so possessing the raw refresh token (validated purely by its
    // hash matching an active, unrevoked row) is the credential here. Rate-limited the same as
    // login/register since it's an equally attractive target to hammer.
    [HttpPost("refresh")]
    [EnableRateLimiting(ApiServiceCollectionExtensions.AuthRateLimitPolicy)]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    // Revokes the given refresh token so it can't be used to mint another access token - always
    // reports success (see LogoutHandler) regardless of whether the token was found, since the
    // caller's goal is already achieved either way.
    [HttpPost("logout")]
    [EnableRateLimiting(ApiServiceCollectionExtensions.AuthRateLimitPolicy)]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToLogoutCommand(), cancellationToken);
        return result.ToActionResult();
    }

    // Issues a fresh token scoped to a different organization the caller is an active member of
    // - the JWT's org_id claim is the only thing that establishes ambient tenant anywhere in the
    // app, so "switching organizations" fundamentally means getting a new token, not a
    // server-side session change.
    [HttpPost("switch-organization")]
    [Authorize]
    public async Task<IActionResult> SwitchOrganization(
        SwitchOrganizationRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }
}
