using SupplierOnboarding.Domain.Proveedores;

namespace SupplierOnboarding.UnitTests.Domain;

public class CatalogoPaisesIso3166Tests
{
    [Theory]
    [InlineData("AR")]
    [InlineData("US")]
    [InlineData("DE")]
    [InlineData("JP")]
    public void EsValido_ConCodigoDePaisSoberanoReconocido_DevuelveTrue(string codigoPais)
    {
        Assert.True(CatalogoPaisesIso3166.EsValido(codigoPais));
    }

    [Theory]
    [InlineData("ZZ")] // código inexistente
    [InlineData("HK")] // Hong Kong: territorio no soberano (Región Administrativa Especial de China)
    [InlineData("PR")] // Puerto Rico: territorio dependiente, no soberano
    [InlineData("")]
    [InlineData(null)]
    public void EsValido_ConTerritorioDependienteOCodigoInvalido_DevuelveFalse(string? codigoPais)
    {
        Assert.False(CatalogoPaisesIso3166.EsValido(codigoPais));
    }
}
