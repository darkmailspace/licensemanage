# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["LicenseManager.sln", "./"]
COPY ["src/LicenseManager.API/LicenseManager.API.csproj", "src/LicenseManager.API/"]
COPY ["src/LicenseManager.Application/LicenseManager.Application.csproj", "src/LicenseManager.Application/"]
COPY ["src/LicenseManager.Domain/LicenseManager.Domain.csproj", "src/LicenseManager.Domain/"]
COPY ["src/LicenseManager.Infrastructure/LicenseManager.Infrastructure.csproj", "src/LicenseManager.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/LicenseManager.API/LicenseManager.API.csproj"

# Copy remaining source code
COPY . .

# Build and publish
WORKDIR "/src/src/LicenseManager.API"
RUN dotnet build "LicenseManager.API.csproj" -c Release -o /app/build
RUN dotnet publish "LicenseManager.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published files
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "LicenseManager.API.dll"]
