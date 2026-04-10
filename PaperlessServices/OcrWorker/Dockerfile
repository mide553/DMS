# -------- Base runtime image --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Install required native OCR dependencies
RUN apt-get update && apt-get install -y \
    tesseract-ocr \
    libleptonica-dev \
    libtesseract-dev \
    tesseract-ocr-eng \
    ghostscript \
    && rm -rf /var/lib/apt/lists/*

RUN ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so
WORKDIR /app/x64
RUN ln -s /usr/lib/x86_64-linux-gnu/liblept.so.5 /app/x64/libleptonica-1.82.0.so
RUN ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /app/x64/libtesseract50.so
WORKDIR /app

# -------- Build stage --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first
COPY Directory.Packages.props .
COPY ["PaperlessModels/", "PaperlessModels/"]
COPY ["PaperlessServices/OcrWorker/", "PaperlessServices/OcrWorker/"]

# Restore
RUN dotnet restore "PaperlessServices/OcrWorker/OcrWorker.csproj"

# Build
WORKDIR "/src/PaperlessServices/OcrWorker"
RUN dotnet build "./OcrWorker.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "./OcrWorker.csproj" -c Release -o /app/publish

# -------- Final stage --------
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OcrWorker.dll"]
