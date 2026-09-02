namespace SupplierOnboarding.Domain.Proveedores;

/// <summary>
/// Catálogo estático de países válidos (FR-003): códigos ISO 3166-1 alpha-2 de los 193 Estados
/// miembros de las Naciones Unidas, reconocidos internacionalmente como entidades soberanas.
/// Excluye deliberadamente territorios dependientes y entidades no soberanas (por ejemplo, Hong
/// Kong, Puerto Rico) aunque tengan asignado un código ISO 3166-1 propio. Sin dependencias de
/// infraestructura ni de servicios externos.
/// </summary>
public static class CatalogoPaisesIso3166
{
    private static readonly HashSet<string> CodigosValidos = new(StringComparer.Ordinal)
    {
        "AF", "AL", "DZ", "AD", "AO", "AG", "AR", "AM", "AU", "AT",
        "AZ", "BS", "BH", "BD", "BB", "BY", "BE", "BZ", "BJ", "BT",
        "BO", "BA", "BW", "BR", "BN", "BG", "BF", "BI", "CV", "KH",
        "CM", "CA", "CF", "TD", "CL", "CN", "CO", "KM", "CG", "CD",
        "CR", "CI", "HR", "CU", "CY", "CZ", "DK", "DJ", "DM", "DO",
        "EC", "EG", "SV", "GQ", "ER", "EE", "SZ", "ET", "FJ", "FI",
        "FR", "GA", "GM", "GE", "DE", "GH", "GR", "GD", "GT", "GN",
        "GW", "GY", "HT", "HN", "HU", "IS", "IN", "ID", "IR", "IQ",
        "IE", "IL", "IT", "JM", "JP", "JO", "KZ", "KE", "KI", "KP",
        "KR", "KW", "KG", "LA", "LV", "LB", "LS", "LR", "LY", "LI",
        "LT", "LU", "MG", "MW", "MY", "MV", "ML", "MT", "MH", "MR",
        "MU", "MX", "FM", "MD", "MC", "MN", "ME", "MA", "MZ", "MM",
        "NA", "NR", "NP", "NL", "NZ", "NI", "NE", "NG", "MK", "NO",
        "OM", "PK", "PW", "PA", "PG", "PY", "PE", "PH", "PL", "PT",
        "QA", "RO", "RU", "RW", "KN", "LC", "VC", "WS", "SM", "ST",
        "SA", "SN", "RS", "SC", "SL", "SG", "SK", "SI", "SB", "SO",
        "ZA", "SS", "ES", "LK", "SD", "SR", "SE", "CH", "SY", "TJ",
        "TZ", "TH", "TL", "TG", "TO", "TT", "TN", "TR", "TM", "TV",
        "UG", "UA", "AE", "GB", "US", "UY", "UZ", "VU", "VE", "VN",
        "YE", "ZM", "ZW",
    };

    public static bool EsValido(string? codigoPais) =>
        !string.IsNullOrWhiteSpace(codigoPais) && CodigosValidos.Contains(codigoPais);
}
