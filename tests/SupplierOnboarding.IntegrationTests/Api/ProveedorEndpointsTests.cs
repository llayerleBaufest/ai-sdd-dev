using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using SupplierOnboarding.Api.Proveedores;
using SupplierOnboarding.Application.Identidad;
using SupplierOnboarding.Domain.Proveedores;
using SupplierOnboarding.Infrastructure.Persistencia;
using SupplierOnboarding.IntegrationTests.Persistencia;

namespace SupplierOnboarding.IntegrationTests.Api;

/// <summary>
/// Pruebas de integración end-to-end del único endpoint <c>POST /api/proveedores</c> contra la
/// base de datos real del contenedor (ADR-0006). <see cref="IUsuarioActual"/> se sustituye por un
/// doble de prueba con un identificador de usuario fijo (no hay autenticación real en esta
/// funcionalidad, corrección U1); <see cref="TimeProvider"/> se sustituye por un
/// <see cref="FakeTimeProvider"/> con instante fijo para aserciones determinísticas.
/// </summary>
[Collection(SqlServerContainerCollection.NombreColeccion)]
public sealed class ProveedorEndpointsTests : IAsyncLifetime
{
    private const string IdentificadorUsuarioDePrueba = "usuario.prueba";
    private static readonly DateTimeOffset InstanteDePrueba = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    private readonly SqlServerContainerFixture _fixtureSqlServer;
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;

    public ProveedorEndpointsTests(SqlServerContainerFixture fixtureSqlServer)
    {
        _fixtureSqlServer = fixtureSqlServer;
    }

    public Task InitializeAsync()
    {
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuracion) =>
            {
                configuracion.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:SupplierOnboarding"] = _fixtureSqlServer.CadenaConexion,
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUsuarioActual>();
                services.AddScoped<IUsuarioActual>(_ => new UsuarioActualDePrueba(IdentificadorUsuarioDePrueba));

                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FakeTimeProvider(InstanteDePrueba));
            });
        });

        _cliente = _fabrica.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _cliente.Dispose();
        _fabrica.Dispose();
        return Task.CompletedTask;
    }

    private static RegistrarProveedorSolicitud SolicitudValida(
        string pais = "AR",
        string identificadorFiscal = "30-12345678-9",
        string razonSocial = "Acme S.A.") =>
        new(razonSocial, pais, identificadorFiscal, "Jane Doe", "jane.doe@acme.com");

    [Fact]
    public async Task PostProveedores_ConDatosValidos_Devuelve201ConProveedorPendienteYAuditoria()
    {
        var solicitud = SolicitudValida(identificadorFiscal: $"T053-{Guid.NewGuid():N}");

        var respuestaHttp = await _cliente.PostAsJsonAsync("/api/proveedores", solicitud);

        Assert.Equal(HttpStatusCode.Created, respuestaHttp.StatusCode);
        var proveedor = await respuestaHttp.Content.ReadFromJsonAsync<ProveedorRespuesta>();
        Assert.NotNull(proveedor);
        Assert.NotEqual(Guid.Empty, proveedor!.Id);
        Assert.Equal("Pendiente", proveedor.Estado);
        Assert.Equal(IdentificadorUsuarioDePrueba, proveedor.RegistradoPor);
        Assert.Equal(InstanteDePrueba, proveedor.RegistradoEn);
    }

    [Fact]
    public async Task PostProveedores_ConDatosValidos_PersisteRealmenteElProveedorEnLaBaseDeDatos()
    {
        var solicitud = SolicitudValida(identificadorFiscal: $"T053B-{Guid.NewGuid():N}");

        var respuestaHttp = await _cliente.PostAsJsonAsync("/api/proveedores", solicitud);
        var proveedor = await respuestaHttp.Content.ReadFromJsonAsync<ProveedorRespuesta>();

        var opciones = new DbContextOptionsBuilder<SupplierOnboardingDbContext>()
            .UseSqlServer(_fixtureSqlServer.CadenaConexion)
            .Options;
        await using var dbContext = new SupplierOnboardingDbContext(opciones);
        var persistido = await dbContext.Proveedores.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == proveedor!.Id);

        Assert.NotNull(persistido);
        Assert.Equal(solicitud.IdentificadorFiscal, persistido!.IdentificadorFiscal);
        Assert.Equal(IdentificadorUsuarioDePrueba, persistido.RegistradoPor);
        Assert.Equal(InstanteDePrueba, persistido.RegistradoEn);
    }

    [Fact]
    public async Task PostProveedores_ConRazonSocialVacia_Devuelve400ConUnSoloErrorDeRazonSocial()
    {
        var solicitud = SolicitudValida() with { RazonSocial = "   " };

        var respuestaHttp = await _cliente.PostAsJsonAsync("/api/proveedores", solicitud);

        Assert.Equal(HttpStatusCode.BadRequest, respuestaHttp.StatusCode);
        var errores = await respuestaHttp.Content.ReadFromJsonAsync<ErroresValidacion>();
        Assert.NotNull(errores);
        Assert.Single(errores!.Errores);
        Assert.Contains("razón social", errores.Errores[0].Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostProveedores_ConCincoDatosObligatoriosInvalidosSimultaneamente_Devuelve400ConCincoErrores()
    {
        var solicitud = new RegistrarProveedorSolicitud("   ", "ZZ", "", "", "no-es-un-correo");

        var respuestaHttp = await _cliente.PostAsJsonAsync("/api/proveedores", solicitud);

        Assert.Equal(HttpStatusCode.BadRequest, respuestaHttp.StatusCode);
        var errores = await respuestaHttp.Content.ReadFromJsonAsync<ErroresValidacion>();
        Assert.NotNull(errores);
        Assert.Equal(5, errores!.Errores.Count);
    }

    [Fact]
    public async Task PostProveedores_ConMismaCombinacionExactaDePaisEIdentificadorFiscal_Devuelve409SinDatosDelExistente()
    {
        var identificadorFiscal = $"T055-{Guid.NewGuid():N}";
        var primeraSolicitud = SolicitudValida(identificadorFiscal: identificadorFiscal);
        var primeraRespuesta = await _cliente.PostAsJsonAsync("/api/proveedores", primeraSolicitud);
        Assert.Equal(HttpStatusCode.Created, primeraRespuesta.StatusCode);

        var segundaRespuesta = await _cliente.PostAsJsonAsync("/api/proveedores", primeraSolicitud);

        Assert.Equal(HttpStatusCode.Conflict, segundaRespuesta.StatusCode);
        var cuerpo = await segundaRespuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain(identificadorFiscal, cuerpo);
        var error = await segundaRespuesta.Content.ReadFromJsonAsync<ErrorDuplicado>();
        Assert.NotNull(error);
        Assert.False(string.IsNullOrWhiteSpace(error!.Mensaje));
    }

    [Fact]
    public async Task PostProveedores_ConVarianteDeFormatoIrrelevanteDelIdentificadorFiscal_Devuelve409()
    {
        var sufijo = Guid.NewGuid().ToString("N");
        var identificadorFiscal = $"T056-{sufijo}";
        var primeraSolicitud = SolicitudValida(identificadorFiscal: identificadorFiscal);
        var primeraRespuesta = await _cliente.PostAsJsonAsync("/api/proveedores", primeraSolicitud);
        Assert.Equal(HttpStatusCode.Created, primeraRespuesta.StatusCode);

        var identificadorFiscalVariante = $"t-0 5 6-{sufijo}";
        var segundaSolicitud = SolicitudValida(identificadorFiscal: identificadorFiscalVariante);
        var segundaRespuesta = await _cliente.PostAsJsonAsync("/api/proveedores", segundaSolicitud);

        Assert.Equal(HttpStatusCode.Conflict, segundaRespuesta.StatusCode);
    }

    [Fact]
    public async Task PostProveedores_ConMismoIdentificadorFiscalEnPaisesDistintos_DevuelveAmbosComoEntidadesIndependientes()
    {
        var identificadorFiscal = $"T057-{Guid.NewGuid():N}";
        var respuestaArgentina = await _cliente.PostAsJsonAsync(
            "/api/proveedores", SolicitudValida(pais: "AR", identificadorFiscal: identificadorFiscal));
        var respuestaBrasil = await _cliente.PostAsJsonAsync(
            "/api/proveedores", SolicitudValida(pais: "BR", identificadorFiscal: identificadorFiscal));

        Assert.Equal(HttpStatusCode.Created, respuestaArgentina.StatusCode);
        Assert.Equal(HttpStatusCode.Created, respuestaBrasil.StatusCode);
    }

    [Fact]
    public async Task PostProveedores_ConMismaRazonSocialYDistintoPaisEIdentificador_DevuelveAmbosComo201()
    {
        var razonSocial = $"Misma Razón Social {Guid.NewGuid():N}";
        var primeraRespuesta = await _cliente.PostAsJsonAsync(
            "/api/proveedores",
            SolicitudValida(razonSocial: razonSocial, pais: "AR", identificadorFiscal: $"T058A-{Guid.NewGuid():N}"));
        var segundaRespuesta = await _cliente.PostAsJsonAsync(
            "/api/proveedores",
            SolicitudValida(razonSocial: razonSocial, pais: "BR", identificadorFiscal: $"T058B-{Guid.NewGuid():N}"));

        Assert.Equal(HttpStatusCode.Created, primeraRespuesta.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segundaRespuesta.StatusCode);
    }

    [Fact]
    public async Task PostProveedores_AnteFallaTecnicaDePersistenciaDistintaDeDuplicado_DevuelveErrorGenericoSinFiltrarDetallesYSinPersistirNada()
    {
        var identificadorFiscal = $"T076-{Guid.NewGuid():N}";

        using var fabricaConFallaTecnica = _fabrica.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IProveedorRepository>();
                services.AddScoped<IProveedorRepository, ProveedorRepositoryFallaTecnica>();
            });
        });
        using var clienteConFallaTecnica = fabricaConFallaTecnica.CreateClient();

        var respuestaHttp = await clienteConFallaTecnica.PostAsJsonAsync(
            "/api/proveedores", SolicitudValida(identificadorFiscal: identificadorFiscal));

        Assert.True(
            (int)respuestaHttp.StatusCode >= 500,
            $"Se esperaba un error de servidor (5xx), pero se recibió {(int)respuestaHttp.StatusCode}.");

        var cuerpo = await respuestaHttp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SqlException", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Microsoft.Data.SqlClient", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.Data", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(nameof(InvalidOperationException), cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", cuerpo);

        // FR-021: el proveedor no debe considerarse registrado, ni parcial ni exitosamente.
        var opciones = new DbContextOptionsBuilder<SupplierOnboardingDbContext>()
            .UseSqlServer(_fixtureSqlServer.CadenaConexion)
            .Options;
        await using var dbContext = new SupplierOnboardingDbContext(opciones);
        var existe = await dbContext.Proveedores.AnyAsync(p => p.IdentificadorFiscal == identificadorFiscal);
        Assert.False(existe);
    }

    private sealed class ProveedorRepositoryFallaTecnica : IProveedorRepository
    {
        public Task<bool> ExisteAsync(string pais, string identificadorFiscalNormalizado, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<ResultadoAlmacenamientoProveedor> AgregarAsync(Proveedor proveedor, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(
                "Simulación de falla técnica de persistencia (T076/FR-021), distinta de un conflicto por duplicado.");
    }

    private sealed class UsuarioActualDePrueba : IUsuarioActual
    {
        public UsuarioActualDePrueba(string identificadorUsuario) => IdentificadorUsuario = identificadorUsuario;

        public string IdentificadorUsuario { get; }
    }
}
