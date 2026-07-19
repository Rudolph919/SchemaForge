namespace SchemaForge.Application.Organizations;

// Enforces the Step 3 §4 last-Owner invariant. Deliberately not a Domain concept: it needs to
// query active Owner count, which requires persistence access Domain can't have (Step 1 §2) - so
// it's an Application-layer check performed before calling OrganizationMembership.Revoke()/
// ChangeRole(), not something the aggregate itself can enforce.
public interface IOrganizationOwnershipGuard
{
    // True if the organization would still have at least one OTHER active Owner if
    // membershipIdBeingChanged were revoked or demoted away from Owner right now.
    Task<bool> HasAnotherActiveOwnerAsync(
        Guid organizationId, Guid membershipIdBeingChanged, CancellationToken cancellationToken);
}
