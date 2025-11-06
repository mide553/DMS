using OcrWorker;
using OcrWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Worker
builder.Services.AddHostedService<Worker>();

// MinIO
builder.Services.AddScoped<IDocumentStorageService, MinIOService>();

var host = builder.Build();
host.Run();
