FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
RUN apk add git
RUN apk add --no-cache nodejs-current npm
USER $APP_UID
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 9103

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RepoAPI/RepoAPI.csproj", "RepoAPI/"]
RUN dotnet restore "RepoAPI/RepoAPI.csproj"
COPY . .
WORKDIR "/src/RepoAPI"
RUN dotnet build "./RepoAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RepoAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RepoAPI.dll"]
