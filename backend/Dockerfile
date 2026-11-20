# ---------- Compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar primero (capa en caché mientras no cambie el .csproj)
COPY backend.csproj .
RUN dotnet restore backend.csproj

COPY . .
RUN dotnet publish backend.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Ejecución ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Render envía el tráfico al puerto 10000 por defecto.
# gcServer=0 reduce el consumo de memoria en instancias pequeñas (plan Free: 512 MB).
ENV ASPNETCORE_HTTP_PORTS=10000 \
    DOTNET_gcServer=0
EXPOSE 10000

COPY --from=build /app/publish .

# Seguridad: el contenedor no corre como root (usuario "app" de la imagen oficial).
USER $APP_UID

ENTRYPOINT ["dotnet", "VmtFabric.Api.dll"]
