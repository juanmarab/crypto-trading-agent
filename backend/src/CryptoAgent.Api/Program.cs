using CryptoAgent.Infrastructure;
using CryptoAgent.Api.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "CryptoAgent API", Version = "v1" });
});

// SignalR (real-time price streaming)
builder.Services.AddSignalR();

// Infrastructure (EF Core + Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// CORS (allow React dev server)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",  // Vite default
                "http://localhost:3000"   // CRA/Next default
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// PriceHub dispatcher (bridges BinanceService events to SignalR clients)
builder.Services.AddHostedService<PriceHubDispatcher>();

var app = builder.Build();

// ── Auto-apply EF Migrations on startup ───────────────────────────────────
// Safe to run every time: EF only applies *pending* migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CryptoAgent.Infrastructure.Data.AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var retries = 5;
    while (retries > 0)
    {
        try
        {
            logger.LogInformation("Applying EF Core migrations...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migration completed successfully.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning(ex, "DB not ready yet. Retrying in 5s... ({Retries} attempts left)", retries);
            if (retries == 0) throw;
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

// ── Pipeline ──────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CryptoAgent API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();
app.MapHub<PriceHub>("/hubs/price");

app.Run();
