using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Queries.ListMyMemberships;

public sealed class ListMyMembershipsHandler(
    IOrganizationMembershipRepository membershipRepository, ICurrentUserContext currentUserContext)
    : IRequestHandler<ListMyMembershipsQuery, Result<IReadOnlyList<MembershipWithOrganizationSummary>>>
{
    public async Task<Result<IReadOnlyList<MembershipWithOrganizationSummary>>> Handle(
        ListMyMembershipsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserContext.UserId!.Value;
        var memberships = await membershipRepository.GetAllByUserIdAsync(userId, cancellationToken);

        return Result<IReadOnlyList<MembershipWithOrganizationSummary>>.Success(memberships);
    }
}
