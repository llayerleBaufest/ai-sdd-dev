using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using SupplierOnboarding.Application.Proveedores.RegistrarProveedor;
using SupplierOnboarding.Domain.Proveedores;

namespace SupplierOnboarding.UnitTests.Application.RegistrarProveedor;

public class RegistrarProveedorCasoDeUsoTests
{
    private static readonly DateTimeOffset InstanteFijo = new(2026, 9, 2, 10, 30, 0, TimeSpan.Zero);

    private const string UsuarioFijo = "usuario.autorizado";

    private static RegistrarProveedorComando ComandoValido(
        string razonSocial = "Acme S.A.",
        string pais = "AR",
        string identificadorFiscal = "30-12345678-9",
        string nombreContacto = "Jane Doe",
        string correoContacto = "jane.doe@acme.com") =>
        new(razonSocial, pais, identificadorFiscal, nombreContacto, correoContacto);

    private static RegistrarProveedorCasoDeUso CrearCasoDeUso(
        ProveedorRepositoryFake repositorio, DateTimeOffset? instante = null, string usuario = UsuarioFijo) =>
        new(
            repositorio,
            new UsuarioActualFake(usuario),
            new FakeTimeProvider(instante ?? InstanteFijo),
            NullLogger<RegistrarProveedorCasoDeUso>.Instance);

    [Fact]
    public async Task EjecutarAsync_ConDatosValidos_DevuelveExitoConProveedorPendienteYDatosDeterministicos()
    {
        var repositorio = new ProveedorRepositoryFake();
        var casoDeUso = CrearCasoDeUso(repositorio);

        var resultado = await casoDeUso.EjecutarAsync(ComandoValido(), CancellationToken.None);

        var exito = Assert.IsType<RegistrarProveedorResultado.Exito>(resultado);
        Assert.Equal(EstadoProveedor.Pendiente, exito.Proveedor.Estado);
        Assert.Equal(UsuarioFijo, exito.Proveedor.RegistradoPor);
        Assert.Equal(InstanteFijo, exito.Proveedor.RegistradoEn);
        Assert.NotEqual(Guid.Empty, exito.Proveedor.Id);
        Assert.Equal(1, repositorio.VecesInvocadoAgregarAsync);
    }

    [Fact]
    public async Task EjecutarAsync_CuandoExisteAsyncDevuelveTrue_DevuelveDuplicadoSinInvocarAgregarAsync()
    {
        // FR-009: un proveedor ya existente con el mismo (país, identificador fiscal normalizado)
        // se detecta antes de intentar persistir.
        var repositorio = new ProveedorRepositoryFake();
        var casoDeUso = CrearCasoDeUso(repositorio);
        await casoDeUso.EjecutarAsync(ComandoValido(), CancellationToken.None);

        var resultado = await casoDeUso.EjecutarAsync(ComandoValido(), CancellationToken.None);

        Assert.IsType<RegistrarProveedorResultado.Duplicado>(resultado);
        Assert.Equal(1, repositorio.VecesInvocadoAgregarAsync);
    }

    [Fact]
    public async Task EjecutarAsync_CuandoAgregarAsyncDevuelveConflictoDuplicado_DevuelveDuplicadoSinLanzarExcepcion()
    {
        // ADR-0005: condición de carrera detectada recién al persistir (índice único en BD),
        // aunque la verificación previa (ExisteAsync) no haya encontrado duplicado.
        var repositorio = new ProveedorRepositoryFake { ForzarConflictoDuplicadoAlAgregar = true };
        var casoDeUso = CrearCasoDeUso(repositorio);

        var resultado = await casoDeUso.EjecutarAsync(ComandoValido(), CancellationToken.None);

        Assert.IsType<RegistrarProveedorResultado.Duplicado>(resultado);
    }

    [Fact]
    public async Task EjecutarAsync_ConDatosInvalidos_DevuelveErroresValidacionSinConsultarElRepositorio()
    {
        var repositorio = new ProveedorRepositoryFake();
        var casoDeUso = CrearCasoDeUso(repositorio);

        var resultado = await casoDeUso.EjecutarAsync(
            ComandoValido(razonSocial: "", pais: "ZZ", identificadorFiscal: "", nombreContacto: "", correoContacto: "no-es-un-correo"),
            CancellationToken.None);

        var errores = Assert.IsType<RegistrarProveedorResultado.ErroresValidacion>(resultado);
        Assert.Equal(5, errores.Errores.Count);
        Assert.Equal(0, repositorio.VecesInvocadoExisteAsync);
        Assert.Equal(0, repositorio.VecesInvocadoAgregarAsync);
    }

    [Fact]
    public async Task EjecutarAsync_ConIdentificadorFiscalConFormatoIrrelevante_ConsultaElRepositorioConElValorNormalizado()
    {
        // FR-010: la detección de duplicados usa el identificador fiscal normalizado, no el
        // original tal como fue ingresado.
        var repositorio = new ProveedorRepositoryFake();
        var casoDeUso = CrearCasoDeUso(repositorio);

        await casoDeUso.EjecutarAsync(ComandoValido(identificadorFiscal: "30-12345678-9"), CancellationToken.None);

        Assert.Equal("30123456789", repositorio.UltimoIdentificadorFiscalNormalizadoConsultado);
    }

    [Fact]
    public async Task EjecutarAsync_ConMismoIdentificadorFiscalTextualEnPaisesDistintos_NoSeConsideraDuplicado()
    {
        // FR-011/SC-006: la unicidad es por (país, identificador fiscal normalizado), no solo por
        // identificador fiscal.
        var repositorio = new ProveedorRepositoryFake();
        var casoDeUso = CrearCasoDeUso(repositorio);
        await casoDeUso.EjecutarAsync(ComandoValido(pais: "AR", identificadorFiscal: "30-12345678-9"), CancellationToken.None);

        var resultado = await casoDeUso.EjecutarAsync(
            ComandoValido(pais: "UY", identificadorFiscal: "30-12345678-9"), CancellationToken.None);

        Assert.IsType<RegistrarProveedorResultado.Exito>(resultado);
        Assert.Equal(2, repositorio.VecesInvocadoAgregarAsync);
    }
}
