using System.Text;
using SchemaForge.Domain.Schemas;

namespace SchemaForge.Application.Schemas.Generation;

public sealed class MarkdownDocumentationRenderer : IDocumentationRenderer
{
    public string FormatKey => "markdown";

    public Task<string> RenderAsync(SchemaVersion version, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.Append("# Schema Documentation\n\n");
        sb.Append($"**Version:** {version.VersionNumber} &nbsp; **Status:** {version.Status}\n\n");
        if (version.ChangeSummary is not null) sb.Append($"> {version.ChangeSummary}\n\n");

        foreach (var field in DocumentationModelBuilder.Build(version))
        {
            var indent = new string(' ', field.Depth * 2);
            var name = field.PropertyName ?? "(root)";
            var flags = string.Join(", ", new[]
            {
                field.Required ? "required" : null,
                field.Nullable ? "nullable" : null,
            }.Where(f => f is not null));

            sb.Append($"{indent}- **{name}** ({field.Kind}{(flags.Length > 0 ? $", {flags}" : "")})");
            if (field.Description is not null) sb.Append($" — {field.Description}");
            sb.Append('\n');

            foreach (var constraint in field.Constraints)
            {
                sb.Append($"{indent}  - _{constraint}_\n");
            }
        }

        return Task.FromResult(sb.ToString());
    }
}
