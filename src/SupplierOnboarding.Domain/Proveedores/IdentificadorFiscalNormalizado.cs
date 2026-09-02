namespace SupplierOnboarding.Domain.Proveedores;

/// <summary>
/// Value object: identificador fiscal normalizado (FR-010), usado únicamente para comparar
/// duplicados y para el índice único de persistencia; nunca se muestra al usuario en lugar del
/// valor original. Operación pura del dominio, sin dependencias de infraestructura.
/// </summary>
public sealed record IdentificadorFiscalNormalizado
{
    public string Valor { get; }

    private IdentificadorFiscalNormalizado(string valor) => Valor = valor;

    /// <summary>
    /// Elimina espacios en blanco, convierte a mayúsculas y elimina los separadores comunes
    /// (<c>-</c>, <c>.</c>, <c>/</c>). Si el resultado queda vacío (por ejemplo, un identificador
    /// compuesto solo por separadores), se conserva como texto vacío: no se inventa aquí una
    /// regla de rechazo adicional no solicitada (ver data-model.md, CHK029).
    /// </summary>
    public static IdentificadorFiscalNormalizado Normalizar(string identificadorFiscalOriginal)
    {
        ArgumentNullException.ThrowIfNull(identificadorFiscalOriginal);

        var sinEspacios = new string(
            identificadorFiscalOriginal.Where(c => !char.IsWhiteSpace(c)).ToArray());

        var enMayusculas = sinEspacios.ToUpperInvariant();

        var sinSeparadores = enMayusculas
            .Replace("-", string.Empty)
            .Replace(".", string.Empty)
            .Replace("/", string.Empty);

        return new IdentificadorFiscalNormalizado(sinSeparadores);
    }

    public override string ToString() => Valor;
}
