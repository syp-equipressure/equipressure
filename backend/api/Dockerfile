# Multi-Stage Dockerfile für EquiApi
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Directory.Build.props zuerst (wichtig für zentrale Build-Konfiguration)
COPY Directory.Build.props* ./

# Copy solution und project files für besseres Layer Caching
COPY EquiApi.slnx ./
COPY EquiApi/EquiApi.csproj ./EquiApi/
COPY EquiApi.Core/EquiApi.Core.csproj ./EquiApi.Core/
COPY EquiApi.Persistence/EquiApi.Persistence.csproj ./EquiApi.Persistence/
COPY EquiApi.Shared/EquiApi.Shared.csproj ./EquiApi.Shared/

# Restore dependencies (wird gecached wenn sich csproj nicht ändern)
RUN dotnet restore ./EquiApi/EquiApi.csproj

# Copy kompletter Source Code
COPY EquiApi/ ./EquiApi/
COPY EquiApi.Core/ ./EquiApi.Core/
COPY EquiApi.Persistence/ ./EquiApi.Persistence/
COPY EquiApi.Shared/ ./EquiApi.Shared/

# Build und Publish
WORKDIR /src/EquiApi
RUN dotnet publish -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Erstelle non-root user für Security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app
COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch zu non-root user
USER appuser

# Expose Port (Standard ASP.NET Core Port)
EXPOSE 8080

# Health check (optional - passe den Pfad an deine API an)
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "EquiApi.dll"]
