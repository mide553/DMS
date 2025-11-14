using GenAIWorker;
using GenAIWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Worker
builder.Services.AddHostedService<Worker>();

// GenAI
builder.Services.AddScoped<ISummarizer, GenAIService>();

var host = builder.Build();
host.Run();
