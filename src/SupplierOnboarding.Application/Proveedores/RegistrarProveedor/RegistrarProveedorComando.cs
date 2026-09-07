namespace SupplierOnboarding.Application.Proveedores.RegistrarProveedor;

/// <summary>
/// Datos de entrada del caso de uso (FR-001). No incluye <c>RegistradoPor</c>: el caso de uso lo
/// obtiene de <see cref="Identidad.IUsuarioActual"/>, nunca del cliente HTTP (corrección U1).
/// </summary>
public sealed record RegistrarProveedorComando(
    string RazonSocial,
    string Pais,
    string IdentificadorFiscal,
    string NombreContacto,
    string CorreoContacto);
