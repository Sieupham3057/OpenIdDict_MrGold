# ============================================================
# BUILD STAGE
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj trước để tận dụng Docker layer cache
# (chỉ re-restore khi csproj thay đổi, không phải mỗi lần sửa code)
COPY ["AuthDemo.Api/AuthDemo.Api.csproj", "AuthDemo.Api/"]
RUN dotnet restore "AuthDemo.Api/AuthDemo.Api.csproj"

# Copy toàn bộ source và publish
COPY . .
RUN dotnet publish "AuthDemo.Api/AuthDemo.Api.csproj" \
    -c Release \
    --no-restore \
    -o /app/publish

# ============================================================
# RUNTIME STAGE — image nhỏ hơn, không có SDK
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Chạy bằng non-root user (security best practice)
RUN groupadd -g 1001 appgroup \
 && useradd -u 1001 -g appgroup -s /bin/false appuser \
 && mkdir -p /app/logs \
 && chown -R appuser:appgroup /app

COPY --from=build --chown=appuser:appgroup /app/publish .

HEALTHCHECK --interval=30s \
            --timeout=10s \
            --start-period=90s \
            --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

USER appuser

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "AuthDemo.Api.dll"]
