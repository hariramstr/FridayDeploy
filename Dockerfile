FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["FridayDeploy.Web/FridayDeploy.Web.csproj", "FridayDeploy.Web/"]
RUN dotnet restore "FridayDeploy.Web/FridayDeploy.Web.csproj"
COPY . .
WORKDIR "/src/FridayDeploy.Web"
RUN dotnet publish "FridayDeploy.Web.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/App_Data
VOLUME ["/app/App_Data"]
HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "FridayDeploy.Web.dll"]
