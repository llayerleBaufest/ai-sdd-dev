namespace SupplierOnboarding.Domain.Proveedores;

/// <summary>
/// Resultado funcional de intentar almacenar un <see cref="Proveedor"/> (ADR-0005). No se modela
/// como excepción de dominio: el conflicto por duplicado es un desenlace esperado del caso de uso
/// (FR-009), no un error técnico.
/// </summary>
public enum ResultadoAlmacenamientoProveedor
{
    Agregado,
    ConflictoDuplicado
}
