# ============================================================
# Dockerfile - Production Ready (.NET 8)
# Project: AuthDemo.Api
# ============================================================

# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj trước để tận dụng Docker cache
COPY ["AuthDemo.Api/AuthDemo.Api.csproj", "AuthDemo.Api/"]

# Nếu có project references thì thêm vào đây
COPY ["AuthDemo.Application/AuthDemo.Application.csproj", "AuthDemo.Application/"]
COPY ["AuthDemo.Infrastructure/AuthDemo.Infrastructure.csproj", "AuthDemo.Infrastructure/"]

# Restore
RUN dotnet restore "AuthDemo.Api/AuthDemo.Api.csproj" \
    --runtime linux-x64 \
    /p:PublishReadyToRun=true

# Copy source code
COPY . .

# Publish
RUN dotnet publish "AuthDemo.Api/AuthDemo.Api.csproj" \
    -c Release \
    -r linux-x64 \
    --no-restore \
    --self-contained false \
    -o /app/publish \
    /p:UseAppHost=false

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Tạo non-root user
RUN groupadd -g 1001 appgroup \
 && useradd -u 1001 -g appgroup -s /bin/false appuser \
 && chown -R appuser:appgroup /app

# Copy publish output
COPY --from=build --chown=appuser:appgroup /app/publish .

# Healthcheck
HEALTHCHECK --interval=30s \
            --timeout=10s \
            --start-period=30s \
            --retries=3 \
    CMD wget -qO- http://localhost:8080/health/live || exit 1

# Chạy bằng user thường
USER appuser

# ASP.NET settings
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

ENTRYPOINT ["dotnet", "AuthDemo.Api.dll"]