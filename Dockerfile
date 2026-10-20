# Image du site peroche.now : compilation avec le SDK .NET 10, exécution sur l'image ASP.NET Core.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restauration d'abord, à partir des seuls fichiers projet : la couche est réutilisée tant qu'ils ne changent pas.
COPY Directory.Build.props ./
COPY src/PerocheNow.Domain/PerocheNow.Domain.csproj src/PerocheNow.Domain/
COPY src/PerocheNow.Business/PerocheNow.Business.csproj src/PerocheNow.Business/
COPY src/PerocheNow.Infrastructure/PerocheNow.Infrastructure.csproj src/PerocheNow.Infrastructure/
COPY src/PerocheNow.Web/PerocheNow.Web.csproj src/PerocheNow.Web/
RUN dotnet restore src/PerocheNow.Web/PerocheNow.Web.csproj

COPY src/ src/
RUN dotnet publish src/PerocheNow.Web/PerocheNow.Web.csproj -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Azure Container Apps termine le HTTPS en amont et transmet X-Forwarded-Proto.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

COPY --from=build /publish .

# Utilisateur non root fourni par l'image officielle ; le port par défaut est 8080.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "PerocheNow.Web.dll"]
