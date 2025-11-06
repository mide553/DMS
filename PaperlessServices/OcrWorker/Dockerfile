# -------- Base runtime image --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Install required native OCR dependencies
RUN apt-get update && apt-get install -y \
    tesseract-ocr \
    libleptonica-dev \
    libtesseract-dev \
    tesseract-ocr-eng \
    && rm -rf /var/lib/apt/lists/*

# -------- Build stage --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PaperlessServices/OcrWorker/OcrWorker.csproj", "PaperlessServices/OcrWorker/"]
RUN dotnet restore "PaperlessServices/OcrWorker/OcrWorker.csproj"

COPY . .
WORKDIR "/src/PaperlessServices/OcrWorker"
RUN dotnet publish "OcrWorker.csproj" -c Release -o /app/publish

# -------- Final stage --------
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OcrWorker.dll"]
