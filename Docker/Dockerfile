# Etapa 1: Base de ejecución (Runtime de .NET 8)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Etapa 2: Compilación (SDK de .NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar el archivo de proyecto y restaurar las dependencias NuGet
COPY ["ITRockChallenge.csproj", "."]
RUN dotnet restore "./ITRockChallenge.csproj"

# Copiar el resto del código y compilar
COPY . .
WORKDIR "/src/."
RUN dotnet build "./ITRockChallenge.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Etapa 3: Publicación optimizada (Publish)
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ITRockChallenge.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Etapa 4: Imagen final limpia y ligera
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ITRockChallenge.dll"]
