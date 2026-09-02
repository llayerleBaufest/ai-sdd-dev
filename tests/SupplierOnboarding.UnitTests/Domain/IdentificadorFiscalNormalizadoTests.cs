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
        // CHK029 (checklist de calidad, caso límite abierto): si tras normalizar el resultado
        // queda vacío, no se inventa aquí una nueva regla de rechazo no solicitada; se documenta
        // el comportamiento actual para que sea revisado explícitamente por el negocio antes de
        // producción (ver data-model.md). FR-004 solo exige que el valor ORIGINAL no esté vacío;
        // este value object no valida eso, es responsabilidad del constructor de Proveedor.
        var normalizado = IdentificadorFiscalNormalizado.Normalizar(identificadorFiscal);

        Assert.Equal(string.Empty, normalizado.Valor);
    }
}
