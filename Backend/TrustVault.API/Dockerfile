FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Backend/TrustVault.API/TrustVault.API.csproj Backend/TrustVault.API/
RUN dotnet restore Backend/TrustVault.API/TrustVault.API.csproj
COPY Backend/TrustVault.API/ Backend/TrustVault.API/
RUN dotnet publish Backend/TrustVault.API/TrustVault.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish/ .
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "TrustVault.API.dll"]
