# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for layer-cache-friendly restore
COPY src/CryptoAgent.Domain/CryptoAgent.Domain.csproj             src/CryptoAgent.Domain/
COPY src/CryptoAgent.Application/CryptoAgent.Application.csproj   src/CryptoAgent.Application/
COPY src/CryptoAgent.Infrastructure/CryptoAgent.Infrastructure.csproj src/CryptoAgent.Infrastructure/
COPY src/CryptoAgent.Api/CryptoAgent.Api.csproj                   src/CryptoAgent.Api/

# Restore only the API project (and its transitive dependencies)
# Does NOT restore the Tests project which doesn't exist in CI/production
RUN dotnet restore src/CryptoAgent.Api/CryptoAgent.Api.csproj

# Copy all source code and publish
COPY src/ src/
RUN dotnet publish src/CryptoAgent.Api/CryptoAgent.Api.csproj \
    -c Release \
    -o /app/publish

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

# ASP.NET listens on 8080 in containers (non-privileged port)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "CryptoAgent.Api.dll"]
