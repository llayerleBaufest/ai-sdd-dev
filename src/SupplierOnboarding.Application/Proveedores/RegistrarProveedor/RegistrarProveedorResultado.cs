using SupplierOnboarding.Domain.Proveedores;

namespace SupplierOnboarding.Application.Proveedores.RegistrarProveedor;

/// <summary>
/// Resultado funcional del caso de uso <c>RegistrarProveedor</c>: distingue registro exitoso,
/// errores de validación y duplicado detectado, sin usar excepciones para representar el
/// duplicado (ADR-0005).
/// </summary>
public abstract record RegistrarProveedorResultado
{
    private RegistrarProveedorResultado()
    {
    }

    public sealed record Exito(Proveedor Proveedor) : RegistrarProveedorResultado;

    public sealed record ErroresValidacion(IReadOnlyList<string> Errores) : RegistrarProveedorResultado;

    public sealed record Duplicado : RegistrarProveedorResultado
    {
        public static readonly Duplicado Instancia = new();
    }
}
