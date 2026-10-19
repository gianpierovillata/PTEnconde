# syntax=docker/dockerfile:1
# =============================================================================
# encode-api — imagen multi-stage para .NET 10
# =============================================================================

# ---- Etapa 1: compilación ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar primero para aprovechar la caché de capas.
COPY ["encode-api.csproj", "./"]
RUN dotnet restore "encode-api.csproj"

COPY . .
RUN dotnet publish "encode-api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---- Etapa 2: runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# El contenedor escucha HTTP; el TLS lo maneja el proxy/ngrok.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

# APP_UID: usuario no root provisto por las imágenes oficiales de .NET 8+.
USER $APP_UID

ENTRYPOINT ["dotnet", "encode-api.dll"]
