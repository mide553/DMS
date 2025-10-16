using OcrWorker;

var builder = Host.CreateApplicationBuilder(args);

// Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
