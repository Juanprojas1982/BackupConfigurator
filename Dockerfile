# Dockerfile para BackupConfigurator API
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/BackupConfigurator.API/BackupConfigurator.API.csproj", "BackupConfigurator.API/"]
COPY ["src/BackupConfigurator.Core/BackupConfigurator.Core.csproj", "BackupConfigurator.Core/"]
COPY ["src/BackupConfigurator.Data/BackupConfigurator.Data.csproj", "BackupConfigurator.Data/"]
RUN dotnet restore "BackupConfigurator.API/BackupConfigurator.API.csproj"
COPY src/ .
WORKDIR "/src/BackupConfigurator.API"
RUN dotnet build "BackupConfigurator.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BackupConfigurator.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BackupConfigurator.API.dll"]
