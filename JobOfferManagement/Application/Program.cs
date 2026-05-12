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
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Puerto dinámico (Render inyecta PORT) ─────────────────────────────
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ── Connection string desde variable de entorno ───────────────────────
// Render > Environment Variables: DB_CONNECTION_STRING = "server=...;user=...;password=...;database=..."
var connectionString =
    Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// ── JWT Secret desde variable de entorno ─────────────────────────────
// Render > Environment Variables: JWT_SECRET = "tu-clave-secreta"
var jwtSecret =
    Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["JwtSettings:Secret"];

// ── Servicios ─────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Jobsy API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingrese el token JWT. Ejemplo: Bearer {token}",
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

// ── CORS: permite Vercel + localhost ──────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── IA ────────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<IChatService, OpenrouterService>();
builder.Services.AddMediatR(typeof(SendMessageHandler).Assembly);
builder.Services.AddScoped<IDocumentAnalyzer, DocumentProcessingService>();

// ── JWT ───────────────────────────────────────────────────────────────
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddHttpContextAccessor();

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
            ValidIssuer = "JobsyIssuer",
            ValidAudience = "JobsyAudience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!)),
            RoleClaimType = "role",
            NameClaimType = JwtRegisteredClaimNames.Sub
        };
    });

builder.Services.AddAuthorization();

// ── MediatR + Servicios de usuario ───────────────────────────────────
builder.Services.AddMediatR(typeof(RegisterUserService).Assembly);
builder.Services.AddScoped<RegisterUserService>();

// ── Base de datos MySQL ───────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySQL(connectionString!);
});

// ── Build ─────────────────────────────────────────────────────────────
var app = builder.Build();

// Migrar / crear tablas al arrancar
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

// ── Pipeline ──────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();