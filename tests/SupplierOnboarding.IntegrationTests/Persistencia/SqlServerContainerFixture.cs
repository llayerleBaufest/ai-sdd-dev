using Microsoft.EntityFrameworkCore;
using SupplierOnboarding.Infrastructure.Persistencia;
using Testcontainers.MsSql;

namespace SupplierOnboarding.IntegrationTests.Persistencia;

/// <summary>
/// Levanta un contenedor SQL Server real (Testcontainers, ADR-0006) una vez por ejecución de la
/// suite y aplica las migraciones de EF Core antes de las pruebas.
/// </summary>
public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _contenedor = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string CadenaConexion => _contenedor.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();

        var opciones = new DbContextOptionsBuilder<SupplierOnboardingDbContext>()
            .UseSqlServer(CadenaConexion)
            .Options;

        await using var dbContext = new SupplierOnboardingDbContext(opciones);
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _contenedor.DisposeAsync();
    }
}

[CollectionDefinition(NombreColeccion)]
public sealed class SqlServerContainerCollection : ICollectionFixture<SqlServerContainerFixture>
{
    public const string NombreColeccion = "SqlServerContainer";
}
