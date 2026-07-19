using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Queries.ListMembers;

public sealed class ListOrganizationMembersHandler(IOrganizationMembershipRepository membershipRepository)
    : IRequestHandler<ListOrganizationMembersQuery, Result<IReadOnlyList<OrganizationMemberSummary>>>
{
    public async Task<Result<IReadOnlyList<OrganizationMemberSummary>>> Handle(
        ListOrganizationMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await membershipRepository.GetAllForCurrentOrganizationAsync(cancellationToken);
        return Result<IReadOnlyList<OrganizationMemberSummary>>.Success(members);
    }
}
