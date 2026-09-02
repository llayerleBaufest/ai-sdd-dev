namespace SupplierOnboarding.Domain.Proveedores;

/// <summary>
/// Estado del proveedor. En el alcance de esta funcionalidad, el único valor posible es
/// <see cref="Pendiente"/> (FR-012, FR-013); no se modelan transiciones futuras.
/// </summary>
public enum EstadoProveedor
{
    Pendiente
}
