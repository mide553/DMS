FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy source code
COPY Directory.Packages.props .
COPY PaperlessServices/BatchProcessor/ PaperlessServices/BatchProcessor/
COPY PaperlessModels/ PaperlessModels/

# Restore dependencies
RUN dotnet restore "PaperlessServices/BatchProcessor/BatchProcessor.csproj"

# Build
WORKDIR "/src/PaperlessServices/BatchProcessor"
RUN dotnet build "BatchProcessor.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "BatchProcessor.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create data directories
RUN mkdir -p /data/input /data/archive

ENTRYPOINT ["dotnet", "BatchProcessor.dll"]
