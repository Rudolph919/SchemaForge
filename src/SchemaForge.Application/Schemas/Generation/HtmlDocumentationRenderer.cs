using System.Net;
using System.Text;
using SchemaForge.Domain.Schemas;

namespace SchemaForge.Application.Schemas.Generation;

public sealed class HtmlDocumentationRenderer : IDocumentationRenderer
{
    public string FormatKey => "html";

    public Task<string> RenderAsync(SchemaVersion version, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.Append("<!doctype html>\n<html><head><meta charset=\"utf-8\"><title>Schema Documentation</title></head><body>\n");
        sb.Append("<h1>Schema Documentation</h1>\n");
        sb.Append($"<p><strong>Version:</strong> {Encode(version.VersionNumber.ToString())} &nbsp; ");
        sb.Append($"<strong>Status:</strong> {Encode(version.Status.ToString())}</p>\n");
        if (version.ChangeSummary is not null)
        {
            sb.Append($"<blockquote>{Encode(version.ChangeSummary)}</blockquote>\n");
        }

        sb.Append("<table border=\"1\" cellpadding=\"4\" cellspacing=\"0\">\n");
        sb.Append("<thead><tr><th>Field</th><th>Kind</th><th>Required</th><th>Nullable</th><th>Description</th><th>Constraints</th></tr></thead>\n<tbody>\n");

        foreach (var field in DocumentationModelBuilder.Build(version))
        {
            var indent = new string(' ', field.Depth * 2); // non-breaking spaces for visual indent
            sb.Append("<tr>");
            sb.Append($"<td>{indent}{Encode(field.PropertyName ?? "(root)")}</td>");
            sb.Append($"<td>{Encode(field.Kind)}</td>");
            sb.Append($"<td>{(field.Required ? "yes" : "")}</td>");
            sb.Append($"<td>{(field.Nullable ? "yes" : "")}</td>");
            sb.Append($"<td>{Encode(field.Description ?? "")}</td>");
            sb.Append($"<td>{Encode(string.Join("; ", field.Constraints))}</td>");
            sb.Append("</tr>\n");
        }

        sb.Append("</tbody>\n</table>\n</body></html>\n");

        return Task.FromResult(sb.ToString());
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
