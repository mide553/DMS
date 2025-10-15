# Stage 1: Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the shared library
COPY PaperlessModels ./PaperlessModels

# Copy OcrWorker project
COPY PaperlessServices/OcrWorker ./PaperlessServices/OcrWorker

# Set working directory to OcrWorker
WORKDIR /src/PaperlessServices/OcrWorker

# Restore dependencies (including PaperlessModels reference)
RUN dotnet restore "OcrWorker.csproj"

# Publish the worker
RUN dotnet publish "OcrWorker.csproj" -c Release -o /app/publish

# Stage 3: Final image
FROM base AS final
WORKDIR /app

# Copy the published output
COPY --from=build /app/publish .

# Set entrypoint
ENTRYPOINT ["dotnet", "OcrWorker.dll"]
