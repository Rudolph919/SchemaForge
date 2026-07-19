using SchemaForge.Application.Identity.Commands.Login;
using SchemaForge.Application.Identity.Commands.RegisterUser;
using SchemaForge.Application.Identity.Commands.SwitchOrganization;
using SchemaForge.Contracts.V1.Auth;

namespace SchemaForge.Api.Mapping;

public static class AuthMappingExtensions
{
    public static RegisterUserCommand ToCommand(this RegisterRequest request) =>
        new(request.Email, request.Password, request.DisplayName, request.OrganizationName);

    public static RegisterResponse ToResponse(this RegisterUserResult result) =>
        new(result.UserId, result.OrganizationId, result.OrganizationSlug);

    public static LoginQuery ToQuery(this LoginRequest request) => new(request.Email, request.Password);

    public static LoginResponse ToResponse(this LoginResult result) =>
        new(result.AccessToken, result.UserId, result.OrganizationId, result.DisplayName);

    public static SwitchOrganizationQuery ToQuery(this SwitchOrganizationRequest request) =>
        new(request.OrganizationId);

    public static SwitchOrganizationResponse ToResponse(this SwitchOrganizationResult result) =>
        new(result.AccessToken, result.OrganizationId, result.DisplayName);
}
