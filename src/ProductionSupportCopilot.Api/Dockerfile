FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore ProductionSupportCopilot.sln --configfile NuGet.Config
RUN dotnet publish src/ProductionSupportCopilot.Api/ProductionSupportCopilot.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProductionSupportCopilot.Api.dll"]
