using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using PaperlessREST;
using PaperlessREST.Controllers;
using PaperlessREST.Data;
using PaperlessREST.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the format: Bearer {your token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// DBContext
builder.Services.AddDbContext<ApplicationDBContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-super-secret-key-minimum-32-characters-long-for-security";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PaperlessAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PaperlessClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebUI", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// DocumentService
builder.Services.AddScoped<IDocumentService, DocumentService>();

// AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// UserService
builder.Services.AddScoped<IUserService, UserService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// MinIO
builder.Services.AddSingleton<IDocumentStorageService, MinIOService>();

// RabbitMQ
builder.Services.AddSingleton<IMessageQueueService, RabbitMQService>();

// ElasticSearch
builder.Services.AddSingleton<ISearchIndexService, ElasticSearchService>();

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

app.UseCors("AllowWebUI");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
