using System.Linq.Expressions;
using Hangfire;
using SchemaForge.Application.Common.Abstractions;

namespace SchemaForge.Infrastructure.BackgroundJobs;

// The one and only place Hangfire's own types (IBackgroundJobClient) show up outside DI wiring -
// everything upstream of this depends only on IJobDispatcher (Step 1 §8).
public sealed class HangfireJobDispatcher(IBackgroundJobClient backgroundJobClient) : IJobDispatcher
{
    public void Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall) =>
        backgroundJobClient.Enqueue(methodCall);
}
