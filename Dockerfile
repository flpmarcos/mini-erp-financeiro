# ============================================================================
# Build multi-stage da aplicacao Contas a Pagar (.NET 8).
# Contexto de build = raiz do projeto (05-contas-a-pagar-net8).
#   docker build -t contas-a-pagar .
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os csproj de todas as camadas primeiro para aproveitar cache de restore.
COPY src/ContasAPagar.Domain/ContasAPagar.Domain.csproj src/ContasAPagar.Domain/
COPY src/ContasAPagar.Infrastructure/ContasAPagar.Infrastructure.csproj src/ContasAPagar.Infrastructure/
COPY src/ContasAPagar.Application/ContasAPagar.Application.csproj src/ContasAPagar.Application/
COPY src/ContasAPagar.Web/ContasAPagar.Web.csproj src/ContasAPagar.Web/
RUN dotnet restore src/ContasAPagar.Web/ContasAPagar.Web.csproj

# Copia o restante e publica.
COPY src/ src/
RUN dotnet publish src/ContasAPagar.Web/ContasAPagar.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# ----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ContasAPagar.Web.dll"]
