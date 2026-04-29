using Jobsy.iam.Infrastructure.Security;
using Jobsy.JobsyAi.Application.Handlers;
using Jobsy.JobsyAi.Domain.Services;
using Jobsy.JobsyAi.Infrastructure.ExternalServices;

using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using Jobsy.UserAuthentication.Application.CommandServices;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Jobsy API", Version = "v1" });

    // Configuración JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingrese el token JWT con el esquema 'Bearer'. Ejemplo: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
//------------------
//-----IA----
builder.Services.AddHttpClient<IChatService, OpenRouterService>();
builder.Services.AddMediatR(typeof(SendMessageHandler).Assembly);
//---document IA--
builder.Services.AddScoped<IDocumentAnalyzer, DocumentProcessingService>();
//------------------JWT---------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddHttpContextAccessor();

var jwtSettingsConfig = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettingsConfig == null)
    throw new InvalidOperationException("JwtSettings no está configurado en appsettings.json");

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettingsConfig.Issuer,
            ValidAudience = jwtSettingsConfig.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettingsConfig.Secret)),
            RoleClaimType = "role",
            NameClaimType = JwtRegisteredClaimNames.Sub
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy",
        policy =>
        {
            policy.WithOrigins("https://front-end-jobsy.vercel.app", "http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


//---
builder.Services.AddMediatR(typeof(RegisterUserService).Assembly);
builder.Services.AddScoped<RegisterUserService>();
builder.Services.AddHttpContextAccessor();
//----
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 36)),
        mysqlOptions => mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(60),
            errorNumbersToAdd: null)
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseCors("ProductionPolicy");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jobsy API V1");
    c.RoutePrefix = string.Empty; // Swagger en la raíz
});

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Initialize database in background (non-blocking)
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(5000); // Wait 5 seconds for container to stabilize
        for (int i = 0; i < 30; i++)
        {
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await context.Database.EnsureCreatedAsync();
                }
                break;
            }
            catch (Exception retryEx)
            {
                if (i < 29)
                {
                    await Task.Delay(2000);
                }
                else
                {
                    throw;
                }
            }
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError($"Background database initialization failed: {ex.Message}");
    }
});

app.MapControllers();

app.Run();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

