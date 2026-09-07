namespace SupplierOnboarding.Api.Proveedores;

/// <summary>
/// Contratos HTTP para "Registrar proveedor" (alineados con contracts/registrar-proveedor.yaml).
/// Sin reglas de negocio, lógica de dominio ni dependencias de Entity Framework Core.
/// </summary>
public sealed record RegistrarProveedorSolicitud(
    string RazonSocial,
    string Pais,
    string IdentificadorFiscal,
    string NombreContacto,
    string CorreoContacto);

public sealed record ProveedorRespuesta(
    Guid Id,
    string RazonSocial,
    string Pais,
    string IdentificadorFiscal,
    string NombreContacto,
    string CorreoContacto,
    string Estado,
    string RegistradoPor,
    DateTimeOffset RegistradoEn);

public sealed record ErrorValidacionItem(string Mensaje);

public sealed record ErroresValidacion(IReadOnlyList<ErrorValidacionItem> Errores);

public sealed record ErrorDuplicado(string Mensaje);
