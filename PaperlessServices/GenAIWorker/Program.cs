using GenAIWorker;
using GenAIWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Worker
builder.Services.AddHostedService<Worker>();

// RabbitMQ
builder.Services.AddSingleton<IMessageQueueService, RabbitMQService>();

// GenAI
builder.Services.AddScoped<ISummarizer, GenAIService>();

var host = builder.Build();
host.Run();
