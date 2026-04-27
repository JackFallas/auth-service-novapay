FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AuthService.sln ./
COPY src/AuthService.Api/AuthService.Api.csproj src/AuthService.Api/
COPY src/AuthService.Application/AuthService.Application.csproj src/AuthService.Application/
COPY src/AuthService.Domain/AuthService.Domain.csproj src/AuthService.Domain/
COPY src/AuthService.Persistence/AuthService.Persistence.csproj src/AuthService.Persistence/

RUN dotnet restore src/AuthService.Api/AuthService.Api.csproj

COPY src/ src/
RUN dotnet publish src/AuthService.Api/AuthService.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/logs /app/keys

COPY --from=build /app/publish .

# El puerto 3000 para el auth-service
ENV ASPNETCORE_URLS=http://+:3000
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ENABLE_SWAGGER=true
# Deshabilitar redirección HTTPS dentro del contenedor (sólo se expone :3000 HTTP).
ENV DISABLE_HTTPS_REDIRECT=true

EXPOSE 3000

ENTRYPOINT ["dotnet", "AuthService.Api.dll"]
