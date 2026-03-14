using Microsoft.EntityFrameworkCore;
using TaskPwa.Server.Data;
using TaskPwa.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers + JSON
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TaskPwa API", Version = "v1" });
    c.AddSecurityDefinition("UserKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-User-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Description = "User partition key (GUID)"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "UserKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// EF Core with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=taskpwa.db";
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlite(connectionString));

// Application services
builder.Services.AddScoped<SyncService>();

// CORS for dev client
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("DevClient", policy =>
        policy
            .WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Ensure DB created in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskPwa API v1"));
}

app.UseHttpsRedirection();
app.UseCors("DevClient");
app.UseAuthorization();
app.MapControllers();

app.Run();
