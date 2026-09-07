using Microsoft.EntityFrameworkCore;
using SupplierOnboarding.Domain.Proveedores;
using SupplierOnboarding.Infrastructure.Persistencia;

namespace SupplierOnboarding.IntegrationTests.Persistencia;

[Collection(SqlServerContainerCollection.NombreColeccion)]
public class ProveedorRepositoryTests
{
    private readonly SqlServerContainerFixture _fixture;

    public ProveedorRepositoryTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private SupplierOnboardingDbContext CrearDbContext()
    {
        var opciones = new DbContextOptionsBuilder<SupplierOnboardingDbContext>()
            .UseSqlServer(_fixture.CadenaConexion)
            .Options;

        return new SupplierOnboardingDbContext(opciones);
    }

    private static Proveedor CrearProveedorValido(string pais, string identificadorFiscal) =>
        new(
            razonSocial: "Acme S.A.",
            pais: pais,
            identificadorFiscal: identificadorFiscal,
            nombreContacto: "Jane Doe",
            correoContacto: "jane.doe@acme.com",
            registradoPor: "usuario.autorizado",
            instante: DateTimeOffset.UtcNow);

    [Fact]
    public async Task AgregarAsync_ConProveedorValido_LoPersisteYExisteAsyncLoEncuentra()
    {
        var identificadorFiscal = $"T050-{Guid.NewGuid():N}";
        await using var dbContext = CrearDbContext();
        var repositorio = new ProveedorRepository(dbContext);
        var proveedor = CrearProveedorValido("AR", identificadorFiscal);

        var resultado = await repositorio.AgregarAsync(proveedor, CancellationToken.None);

        Assert.Equal(ResultadoAlmacenamientoProveedor.Agregado, resultado);

        await using var dbContextConsulta = CrearDbContext();
        var repositorioConsulta = new ProveedorRepository(dbContextConsulta);
        var existe = await repositorioConsulta.ExisteAsync(
            "AR", proveedor.IdentificadorFiscalNormalizado.Valor, CancellationToken.None);

        Assert.True(existe);

        // Verificación directa de los valores persistidos (identificador original vs. normalizado,
        // estado, auditoría), más allá de lo que expone IProveedorRepository.
        var persistido = await dbContextConsulta.Proveedores.AsNoTracking()
            .SingleAsync(p => p.Id == proveedor.Id);

        Assert.Equal(identificadorFiscal, persistido.IdentificadorFiscal);
        Assert.Equal(proveedor.IdentificadorFiscalNormalizado.Valor, persistido.IdentificadorFiscalNormalizado.Valor);
        Assert.Equal(EstadoProveedor.Pendiente, persistido.Estado);
        Assert.Equal(proveedor.RegistradoPor, persistido.RegistradoPor);
        Assert.Equal(proveedor.RegistradoEn, persistido.RegistradoEn);
    }

    [Fact]
    public async Task AgregarAsync_ConPaisesDistintosYMismoIdentificadorFiscalTextual_AmbosSePersisten()
    {
        var identificadorFiscal = $"T050B-{Guid.NewGuid():N}";

        await using var dbContext = CrearDbContext();
        var repositorio = new ProveedorRepository(dbContext);

        var resultadoAr = await repositorio.AgregarAsync(
            CrearProveedorValido("AR", identificadorFiscal), CancellationToken.None);
        var resultadoBr = await repositorio.AgregarAsync(
            CrearProveedorValido("BR", identificadorFiscal), CancellationToken.None);

        Assert.Equal(ResultadoAlmacenamientoProveedor.Agregado, resultadoAr);
        Assert.Equal(ResultadoAlmacenamientoProveedor.Agregado, resultadoBr);
    }

    [Fact]
    public async Task AgregarAsync_ConSegundaInsercionDeMismaCombinacion_DevuelveConflictoDuplicado()
    {
        var identificadorFiscal = $"T051-{Guid.NewGuid():N}";

        await using (var dbContext = CrearDbContext())
        {
            var repositorio = new ProveedorRepository(dbContext);
            var resultado = await repositorio.AgregarAsync(
                CrearProveedorValido("AR", identificadorFiscal), CancellationToken.None);
            Assert.Equal(ResultadoAlmacenamientoProveedor.Agregado, resultado);
        }

        // Variante de formato irrelevante (espacios/mayúsculas/separadores distintos, FR-010).
        var identificadorFiscalVariante = $" {identificadorFiscal.ToLowerInvariant()} ".Replace("t0", "t-0");

        await using (var dbContext = CrearDbContext())
        {
            var repositorio = new ProveedorRepository(dbContext);
            var resultado = await repositorio.AgregarAsync(
                CrearProveedorValido("AR", identificadorFiscalVariante), CancellationToken.None);

            Assert.Equal(ResultadoAlmacenamientoProveedor.ConflictoDuplicado, resultado);
        }
    }

    [Fact]
    public async Task AgregarAsync_ConDosInsercionesConcurrentesDeMismaCombinacion_SoloUnaTieneExito()
    {
        var identificadorFiscal = $"T052-{Guid.NewGuid():N}";

        var tarea1 = Task.Run(async () =>
        {
            await using var dbContext = CrearDbContext();
            var repositorio = new ProveedorRepository(dbContext);
            return await repositorio.AgregarAsync(
                CrearProveedorValido("AR", identificadorFiscal), CancellationToken.None);
        });

        var tarea2 = Task.Run(async () =>
        {
            await using var dbContext = CrearDbContext();
            var repositorio = new ProveedorRepository(dbContext);
            return await repositorio.AgregarAsync(
                CrearProveedorValido("AR", identificadorFiscal), CancellationToken.None);
        });

        var resultados = await Task.WhenAll(tarea1, tarea2);

        Assert.Single(resultados, r => r == ResultadoAlmacenamientoProveedor.Agregado);
        Assert.Single(resultados, r => r == ResultadoAlmacenamientoProveedor.ConflictoDuplicado);
    }
}
