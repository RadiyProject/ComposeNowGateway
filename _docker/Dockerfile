# ============= Base: .NET SDK 9.0 =============
FROM mcr.microsoft.com/dotnet/sdk:9.0.303 AS base

# ============= Runtime/Dev Stage =============
FROM base AS dev

WORKDIR /app
EXPOSE 5001

VOLUME ["/app"]

CMD ["dotnet", "watch", "run", "--project", "/app"]

# ============= Build/Runtime Stage =============
FROM base AS publish

WORKDIR /src
COPY app/ app/

RUN dotnet restore app/ComposeNowGateway.csproj
RUN dotnet publish app/ComposeNowGateway.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app
COPY --from=publish /out .
ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001
ENTRYPOINT ["dotnet", "ComposeNowGateway.dll"]
