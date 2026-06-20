# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore-relevant files first (better layer caching). CPM/shared props live at the root.
COPY global.json Directory.Build.props Directory.Packages.props BudgetPilot.sln ./
COPY src/ ./src/

RUN dotnet restore src/BudgetPilot.Web/BudgetPilot.Web.csproj

# Versions-/Commit-Infos werden vom CI als Build-Args übergeben (im Container gibt
# es kein .git). Ohne Args bleibt es "unknown".
ARG COMMIT_HASH=unknown
ARG COMMIT_DATE=unknown
RUN dotnet publish src/BudgetPilot.Web/BudgetPilot.Web.csproj -c Release -o /app/publish --no-restore \
    /p:UseAppHost=false /p:CommitHash="$COMMIT_HASH" /p:CommitDate="$COMMIT_DATE"

# ── Runtime stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# SQLite database lives in a mounted volume (see docker-compose.yml).
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Database__Provider=Sqlite \
    Database__ConnectionString=Data Source=/app/data/budgetpilot.db
EXPOSE 8080

ENTRYPOINT ["dotnet", "BudgetPilot.Web.dll"]
