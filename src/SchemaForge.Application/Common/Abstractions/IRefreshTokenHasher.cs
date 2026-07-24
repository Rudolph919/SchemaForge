namespace SchemaForge.Application.Common.Abstractions;

public interface IRefreshTokenHasher
{
    string Hash(string rawToken);
}
