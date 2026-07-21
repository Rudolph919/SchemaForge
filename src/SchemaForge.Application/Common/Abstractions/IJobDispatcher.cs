using System.Linq.Expressions;

namespace SchemaForge.Application.Common.Abstractions;

// Application must not reference Hangfire directly (Step 1 §2's layer rule) - this port narrows
// to exactly the one operation this codebase needs (fire-and-forget enqueue), shaped as an
// Expression<Func<TJob, Task>> so it matches Hangfire's own IBackgroundJobClient.Enqueue signature
// closely enough that Infrastructure's implementation is a near-direct pass-through, without
// Application taking a package reference to get there. TJob is resolved from the DI container at
// execution time by whatever sits behind this port - it never needs to be Hangfire-aware itself.
public interface IJobDispatcher
{
    void Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall);
}
