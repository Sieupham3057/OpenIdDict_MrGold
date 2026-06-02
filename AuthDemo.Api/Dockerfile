# ============================================================
# BUILD STAGE
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy csproj trước để tận dụng cache
COPY ["AuthDemo.Api/AuthDemo.Api.csproj", "AuthDemo.Api/"]

RUN dotnet restore "AuthDemo.Api/AuthDemo.Api.csproj"

# Copy source code
COPY . .

# Publish
RUN dotnet publish "AuthDemo.Api/AuthDemo.Api.csproj" \
    -c Release \
    --no-restore \
    -o /app/publish

# ============================================================
# RUNTIME STAGE
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Cài curl để Docker HealthCheck hoạt động
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Tạo user non-root
RUN groupadd -g 1001 appgroup \
    && useradd -u 1001 -g appgroup -s /usr/sbin/nologin appuser \
    && mkdir -p /app/logs /app/certs \
    && chown -R appuser:appgroup /app

# Copy application
COPY --from=build --chown=appuser:appgroup /app/publish .

# ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose container port
EXPOSE 8080

# Health Check
HEALTHCHECK --interval=30s \
            --timeout=10s \
            --start-period=90s \
            --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1

# Chạy bằng user thường
USER appuser

ENTRYPOINT ["dotnet", "AuthDemo.Api.dll"]