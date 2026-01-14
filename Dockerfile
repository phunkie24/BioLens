# BioLens API Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/BioLens.API/BioLens.API.csproj", "BioLens.API/"]
COPY ["src/BioLens.Application/BioLens.Application.csproj", "BioLens.Application/"]
COPY ["src/BioLens.Domain/BioLens.Domain.csproj", "BioLens.Domain/"]
COPY ["src/BioLens.Infrastructure/BioLens.Infrastructure.csproj", "BioLens.Infrastructure/"]
COPY ["src/BioLens.Agents/BioLens.Agents.csproj", "BioLens.Agents/"]

# Restore dependencies
RUN dotnet restore "BioLens.API/BioLens.API.csproj"

# Copy source code
COPY src/ .

# Build application
WORKDIR "/src/BioLens.API"
RUN dotnet build "BioLens.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BioLens.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "BioLens.API.dll"]
