FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props global.json ./
COPY src/SchemaForge.SharedKernel/SchemaForge.SharedKernel.csproj src/SchemaForge.SharedKernel/
COPY src/SchemaForge.Domain/SchemaForge.Domain.csproj src/SchemaForge.Domain/
COPY src/SchemaForge.Application/SchemaForge.Application.csproj src/SchemaForge.Application/
COPY src/SchemaForge.Infrastructure/SchemaForge.Infrastructure.csproj src/SchemaForge.Infrastructure/
COPY src/SchemaForge.Contracts/SchemaForge.Contracts.csproj src/SchemaForge.Contracts/
COPY src/SchemaForge.Api/SchemaForge.Api.csproj src/SchemaForge.Api/
RUN dotnet restore src/SchemaForge.Api/SchemaForge.Api.csproj

COPY src/ src/
RUN dotnet publish src/SchemaForge.Api/SchemaForge.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SchemaForge.Api.dll"]
