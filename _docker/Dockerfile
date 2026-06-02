# ============= Base: .NET SDK 9.0 =============
FROM mcr.microsoft.com/dotnet/sdk:9.0.303 AS base

# ============= Runtime/Dev Stage =============
FROM base AS dev

WORKDIR /app
EXPOSE 5001

VOLUME ["/app"]

CMD ["dotnet", "watch", "run", "--project", "/app"]
