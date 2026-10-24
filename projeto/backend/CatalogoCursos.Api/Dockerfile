# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CatalogoCursos.Api/CatalogoCursos.Api.csproj CatalogoCursos.Api/
RUN dotnet restore CatalogoCursos.Api/CatalogoCursos.Api.csproj
COPY . .
RUN dotnet publish CatalogoCursos.Api/CatalogoCursos.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
# A imagem aspnet:8.0 já escuta em 8080 (ASPNETCORE_HTTP_PORTS=8080)
EXPOSE 8080
ENTRYPOINT ["dotnet", "CatalogoCursos.Api.dll"]
