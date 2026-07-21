namespace SchemaForge.Api.Middleware;

// Marks a POST action as eligible for Idempotency-Key replay (Step 6 §1.6) - opt-in, not applied
// blanket to every POST, since the concern is specifically "a retried request shouldn't
// re-execute a side effect" (creation and state-transition endpoints), not every POST verb (e.g.
// /auth/login, /auth/register, and /validate are POSTs but none of them are the kind of
// accidental-double-fire risk this protects against).
[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute : Attribute;
