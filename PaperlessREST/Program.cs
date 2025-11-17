using Microsoft.EntityFrameworkCore;
using PaperlessREST;
using PaperlessREST.Controllers;
using PaperlessREST.Data;
using PaperlessREST.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DBContext
builder.Services.AddDbContext<ApplicationDBContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// DocumentService
builder.Services.AddScoped<IDocumentService, DocumentService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// MinIO
builder.Services.AddSingleton<IDocumentStorageService, MinIOService>();

// RabbitMQ
builder.Services.AddSingleton<IMessageQueueService, RabbitMQService>();

// Worker
builder.Services.AddHostedService<Worker>();

var app = builder.Build();


// Migration
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
