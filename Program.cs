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

// --- PUERTO Y URL PARA RAILWAY ---
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// --- CONFIGURACIÓN DE CORS ---
// Unificamos las políticas en una sola para evitar conflictos
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy", policy =>
    {
        policy.WithOrigins("https://front-end-jobsy.vercel.app", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Agregado por si usas cookies o auth específico
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Jobsy API", Version = "v1" });
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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// --- SERVICIOS ---
builder.Services.AddHttpClient<IChatService, OpenRouterService>();
builder.Services.AddMediatR(typeof(SendMessageHandler).Assembly);
builder.Services.AddScoped<IDocumentAnalyzer, DocumentProcessingService>();

// --- JWT CONFIG (OPTIMIZADO PARA VARIABLES DE ENTORNO) ---
var jwtSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSection);
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddHttpContextAccessor();

// Extraemos los valores para la validación (esto leerá de Railway automáticamente)
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];
var jwtSecret = jwtSection["Secret"];

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
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            RoleClaimType = "role",
            NameClaimType = JwtRegisteredClaimNames.Sub
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddMediatR(typeof(RegisterUserService).Assembly);
builder.Services.AddScoped<RegisterUserService>();

// --- CONEXIÓN A BASE DE DATOS ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    // Usamos MySQL (asegúrate de que el paquete Pomelo.EntityFrameworkCore.MySql esté instalado)
    options.UseMySQL(connectionString); 
});

var app = builder.Build();

// --- MIGRACIONES AUTOMÁTICAS ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // EnsureCreated es útil para desarrollo, pero si tienes migraciones usa context.Database.Migrate();
    context.Database.EnsureCreated();
}

// --- MIDDLEWARE PIPELINE ---
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jobsy API V1");
    c.RoutePrefix = "swagger";
});

// IMPORTANTE: UseCors debe ir ANTES de Authentication y Authorization
app.UseCors("ProductionPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
