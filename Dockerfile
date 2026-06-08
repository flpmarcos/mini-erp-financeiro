# ============================================================================
# FinFlow — build multi-stage (.NET 8). Contexto de build = raiz do repositório.
#   docker build -t finflow .
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os csproj de todas as camadas primeiro para aproveitar cache de restore.
COPY src/FinFlow.Domain/FinFlow.Domain.csproj src/FinFlow.Domain/
COPY src/FinFlow.Infrastructure/FinFlow.Infrastructure.csproj src/FinFlow.Infrastructure/
COPY src/FinFlow.Application/FinFlow.Application.csproj src/FinFlow.Application/
COPY src/FinFlow.Web/FinFlow.Web.csproj src/FinFlow.Web/
RUN dotnet restore src/FinFlow.Web/FinFlow.Web.csproj

# Copia o restante e publica.
COPY src/ src/
RUN dotnet publish src/FinFlow.Web/FinFlow.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# ----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl: usado pelo HEALTHCHECK do docker-compose (/health).
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Pastas de runtime (anexos/logs) — montadas como volumes no compose.
RUN mkdir -p /app/uploads /app/logs

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "FinFlow.Web.dll"]
