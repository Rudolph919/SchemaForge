using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Domain.Identity;

// Global identity, not tenant-owned - a person's account spans Organizations (Step 2 §5).
// PasswordHash is opaque here; hashing/verification is an Application-layer concern via
// IPasswordHasher, kept out of Domain to avoid a crypto-library dependency on this layer.
public sealed class User : AuditableEntity<Guid>
{
    public EmailAddress Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public bool EmailVerified { get; private set; }

    private User() { } // EF Core materialization

    private User(Guid id, EmailAddress email, string passwordHash, string displayName) : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        EmailVerified = false;
    }

    public static User Register(EmailAddress email, string passwordHash, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));

        var user = new User(Guid.NewGuid(), email, passwordHash, displayName);
        user.RaiseDomainEvent(new UserRegistered(user.Id, email.Value));

        return user;
    }
}
