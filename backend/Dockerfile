# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (layer-cache friendly)
COPY CryptoAgent.sln ./
COPY src/CryptoAgent.Domain/CryptoAgent.Domain.csproj             src/CryptoAgent.Domain/
COPY src/CryptoAgent.Application/CryptoAgent.Application.csproj   src/CryptoAgent.Application/
COPY src/CryptoAgent.Infrastructure/CryptoAgent.Infrastructure.csproj src/CryptoAgent.Infrastructure/
COPY src/CryptoAgent.Api/CryptoAgent.Api.csproj                   src/CryptoAgent.Api/

RUN dotnet restore

# Copy all source code and publish
COPY . .
RUN dotnet publish src/CryptoAgent.Api/CryptoAgent.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

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
