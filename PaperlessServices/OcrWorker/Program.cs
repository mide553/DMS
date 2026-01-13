using OcrWorker;
using OcrWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Worker
builder.Services.AddHostedService<Worker>();

// MinIO
builder.Services.AddScoped<IDocumentStorageService, MinIOService>();

// Tesseract
builder.Services.AddScoped<IDocumentExtractorService, TesseractService>();

// ElasticSearch
builder.Services.AddScoped<ISearchIndexService, ElasticSearchService>();

var host = builder.Build();
host.Run();
