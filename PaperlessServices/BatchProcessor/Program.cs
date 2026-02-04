using BatchProcessor;
using BatchProcessor.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Add DbContext
builder.Services.AddDbContext<BatchDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

// Add services
builder.Services.AddSingleton<IXmlProcessorService, XmlProcessorService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
