using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using SupplierOnboarding.Api.Identidad;
using SupplierOnboarding.Api.Proveedores;
using SupplierOnboarding.Application.Identidad;
using SupplierOnboarding.Application.Proveedores.RegistrarProveedor;
using SupplierOnboarding.Domain.Proveedores;
using SupplierOnboarding.Infrastructure.Persistencia;

var builder = WebApplication.CreateBuilder(args);

// T010: manejo uniforme de errores no controlados (ProblemDetails) para cualquier endpoint futuro.
builder.Services.AddProblemDetails();

// T011: única fuente del instante de registro que Application pasará a Domain (research.md punto 10).
builder.Services.AddSingleton(TimeProvider.System);

// T048: persistencia (EF Core + SQL Server) usando la cadena de conexión de configuración.
builder.Services.AddDbContext<SupplierOnboardingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SupplierOnboarding")));

// T048: IUsuarioActual (corrección U1) requiere HttpContext para la implementación temporal.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualHttp>();

builder.Services.AddScoped<IProveedorRepository, ProveedorRepository>();
builder.Services.AddScoped<RegistrarProveedorCasoDeUso>();

// T061-T063: documentación interactiva de la API en Development (DX, ADR-0009).
builder.Services.AddOpenApi();

// T059: trazas y métricas (ASP.NET Core, EF Core, SqlClient), exportando a consola/OTLP local (ADR-0008).
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // Scalar no queda habilitado por defecto en producción (ADR-0009).
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapProveedorEndpoints();

app.Run();

// Punto de extensión para WebApplicationFactory en pruebas de integración.
public partial class Program
{
}
