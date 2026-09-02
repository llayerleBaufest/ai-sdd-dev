namespace SupplierOnboarding.Domain.Proveedores;

/// <summary>
/// Puerto específico de dominio (Repository Pattern, Principio VII de la Constitution). Expone
/// únicamente las operaciones que el caso de uso <c>RegistrarProveedor</c> necesita; no es un
/// repositorio genérico (<c>GenericRepository&lt;T&gt;</c>).
/// </summary>
public interface IProveedorRepository
{
    /// <summary>
    /// Verifica la regla de negocio cross-aggregate de unicidad (FR-007/FR-008/FR-009) antes de
    /// intentar registrar, para dar una respuesta de negocio clara en el camino feliz.
    /// </summary>
    Task<bool> ExisteAsync(string pais, string identificadorFiscalNormalizado, CancellationToken cancellationToken);

    /// <summary>
    /// Intenta persistir el proveedor. Ante una violación de la restricción única de persistencia
    /// (condición de carrera), devuelve <see cref="ResultadoAlmacenamientoProveedor.ConflictoDuplicado"/>
    /// como resultado funcional, nunca como excepción de dominio (ADR-0005).
    /// </summary>
    Task<ResultadoAlmacenamientoProveedor> AgregarAsync(Proveedor proveedor, CancellationToken cancellationToken);
}
