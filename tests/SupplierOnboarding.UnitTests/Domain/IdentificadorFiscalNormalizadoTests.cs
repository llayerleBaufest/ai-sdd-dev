using SupplierOnboarding.Domain.Proveedores;

namespace SupplierOnboarding.UnitTests.Domain;

public class IdentificadorFiscalNormalizadoTests
{
    [Theory]
    [InlineData("30-12345678-9")]
    [InlineData("30.12345.678-9")]
    [InlineData("30 12345678 9")]
    public void Normalizar_EliminaEspaciosMayusculasYSeparadoresComunes(string identificadorFiscal)
    {
        var normalizado = IdentificadorFiscalNormalizado.Normalizar(identificadorFiscal);

        Assert.DoesNotContain(" ", normalizado.Valor);
        Assert.DoesNotContain("-", normalizado.Valor);
        Assert.DoesNotContain(".", normalizado.Valor);
        Assert.DoesNotContain("/", normalizado.Valor);
        Assert.Equal(normalizado.Valor.ToUpperInvariant(), normalizado.Valor);
    }

    [Theory]
    [InlineData("30-12345678-9", "30 12345678 9")]
    [InlineData("abc123", "ABC123")]
    [InlineData("30.123/456-9", "30123456 9")]
    public void Normalizar_ProduceElMismoResultadoParaVariantesDeFormatoIrrelevantes(string a, string b)
    {
        var normalizadoA = IdentificadorFiscalNormalizado.Normalizar(a);
        var normalizadoB = IdentificadorFiscalNormalizado.Normalizar(b);

        Assert.Equal(normalizadoA, normalizadoB);
    }

    [Theory]
    [InlineData("---")]
    [InlineData("   ")]
    [InlineData("-.-/")]
    public void Normalizar_ConIdentificadorCompuestoSoloPorSeparadoresOEspacios_ProduceTextoVacio(string identificadorFiscal)
    {
        // CHK029 (resuelto, sesión 2026-09-03): esta clase solo calcula la normalización; el
        // rechazo del caso vacío es responsabilidad del constructor de Proveedor (T072) y de
        // RegistrarProveedorValidador (T074), no de este value object.
        var normalizado = IdentificadorFiscalNormalizado.Normalizar(identificadorFiscal);

        Assert.Equal(string.Empty, normalizado.Valor);
    }
}
