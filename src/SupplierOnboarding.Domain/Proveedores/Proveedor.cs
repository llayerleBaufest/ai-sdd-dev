namespace SupplierOnboarding.Domain.Proveedores;

/// <summary>
/// Raíz de agregado. Representa a una organización externa que inicia un proceso de onboarding
/// (spec.md, "Entidades Clave"). El constructor protege sus invariantes esenciales mediante guard
/// clauses independientes de <c>RegistrarProveedorValidador</c> (Application): si la validación de
/// entrada se omitiera o dejara pasar algo por error, esta entidad igualmente rechaza su propia
/// construcción en un estado inválido.
/// </summary>
public sealed class Proveedor
{
    public Guid Id { get; }

    public string RazonSocial { get; }

    public string Pais { get; }

    public string IdentificadorFiscal { get; }

    public IdentificadorFiscalNormalizado IdentificadorFiscalNormalizado { get; }

    public string NombreContacto { get; }

    public string CorreoContacto { get; }

    public EstadoProveedor Estado { get; }

    public string RegistradoPor { get; }

    public DateTimeOffset RegistradoEn { get; }

    /// <param name="instante">
    /// Instante de registro, obtenido por Application mediante <c>TimeProvider</c> (nunca
    /// <c>DateTimeOffset.UtcNow</c>/<c>DateTime.Now</c> dentro de Domain); se usa tanto para
    /// <see cref="RegistradoEn"/> como para generar <see cref="Id"/> con
    /// <c>Guid.CreateVersion7</c> (ADR-0004).
    /// </param>
    public Proveedor(
        string razonSocial,
        string pais,
        string identificadorFiscal,
        string nombreContacto,
        string correoContacto,
        string registradoPor,
        DateTimeOffset instante)
    {
        if (string.IsNullOrWhiteSpace(razonSocial))
        {
            throw new ArgumentException("La razón social no puede estar vacía.", nameof(razonSocial));
        }

        if (!CatalogoPaisesIso3166.EsValido(pais))
        {
            throw new ArgumentException("El país no es válido.", nameof(pais));
        }

        if (string.IsNullOrWhiteSpace(identificadorFiscal))
        {
            throw new ArgumentException("El identificador fiscal no puede estar vacío.", nameof(identificadorFiscal));
        }

        if (string.IsNullOrWhiteSpace(nombreContacto))
        {
            throw new ArgumentException("El nombre de contacto no puede estar vacío.", nameof(nombreContacto));
        }

        if (!EsCorreoValido(correoContacto))
        {
            throw new ArgumentException(
                "El correo electrónico de contacto no tiene un formato válido.", nameof(correoContacto));
        }

        if (string.IsNullOrWhiteSpace(registradoPor))
        {
            throw new ArgumentException("Debe indicarse quién realiza el registro.", nameof(registradoPor));
        }

        Id = Guid.CreateVersion7(instante);
        RazonSocial = razonSocial;
        Pais = pais;
        IdentificadorFiscal = identificadorFiscal;
        IdentificadorFiscalNormalizado = IdentificadorFiscalNormalizado.Normalizar(identificadorFiscal);
        NombreContacto = nombreContacto;
        CorreoContacto = correoContacto;
        Estado = EstadoProveedor.Pendiente;
        RegistradoPor = registradoPor;
        RegistradoEn = instante;
    }

    private static bool EsCorreoValido(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
        {
            return false;
        }

        try
        {
            _ = new System.Net.Mail.MailAddress(correo);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
