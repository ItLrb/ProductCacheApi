FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ProductCacheApi.csproj", "./"]
RUN dotnet restore "ProductCacheApi.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "ProductCacheApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ProductCacheApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish --chown=$APP_UID /app/publish .
# Serilog writes rolling files under ./Logs; make sure the non-root user can create them.
USER root
RUN mkdir -p /app/Logs && chown $APP_UID /app/Logs
USER $APP_UID
ENTRYPOINT ["dotnet", "ProductCacheApi.dll"]
