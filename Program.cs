using DistributionAPI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext with SQL Server
builder.Services.AddDbContext<DistributionDbContext>(options =>
    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=CozyComfortDistribution;Trusted_Connection=True;"));

// Add HttpClient
builder.Services.AddHttpClient();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Distribution API V1");
        c.RoutePrefix = "swagger"; // ✅ Swagger UI සඳහා route
    });
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DistributionDbContext>();
    dbContext.Database.EnsureCreated();
}

// ✅ Browser එක Open කරන message එක
Console.WriteLine("Distribution API is running on: http://localhost:6001");
Console.WriteLine("Swagger UI: http://localhost:6001/swagger");

app.Run();