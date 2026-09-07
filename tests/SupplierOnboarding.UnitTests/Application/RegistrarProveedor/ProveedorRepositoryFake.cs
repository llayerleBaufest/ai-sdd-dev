using SupplierOnboarding.Domain.Proveedores;

namespace SupplierOnboarding.UnitTests.Application.RegistrarProveedor;

/// <summary>
/// Fake en memoria de <see cref="IProveedorRepository"/> para pruebas unitarias, sin biblioteca de
/// mocking (research.md punto 9). Permite forzar <see cref="ResultadoAlmacenamientoProveedor.ConflictoDuplicado"/>
/// en <see cref="AgregarAsync"/> para simular una condición de carrera (ADR-0005).
/// </summary>
public sealed class ProveedorRepositoryFake : IProveedorRepository
{
    private readonly List<Proveedor> _proveedores = [];

    public bool ForzarConflictoDuplicadoAlAgregar { get; set; }

    public int VecesInvocadoExisteAsync { get; private set; }

    public int VecesInvocadoAgregarAsync { get; private set; }

    public string? UltimoPaisConsultado { get; private set; }

    public string? UltimoIdentificadorFiscalNormalizadoConsultado { get; private set; }

    public Task<bool> ExisteAsync(string pais, string identificadorFiscalNormalizado, CancellationToken cancellationToken)
    {
        VecesInvocadoExisteAsync++;
        UltimoPaisConsultado = pais;
        UltimoIdentificadorFiscalNormalizadoConsultado = identificadorFiscalNormalizado;

        var existe = _proveedores.Any(p =>
            p.Pais == pais && p.IdentificadorFiscalNormalizado.Valor == identificadorFiscalNormalizado);
        return Task.FromResult(existe);
    }

    public Task<ResultadoAlmacenamientoProveedor> AgregarAsync(Proveedor proveedor, CancellationToken cancellationToken)
    {
        VecesInvocadoAgregarAsync++;

        if (ForzarConflictoDuplicadoAlAgregar)
        {
            return Task.FromResult(ResultadoAlmacenamientoProveedor.ConflictoDuplicado);
        }

        _proveedores.Add(proveedor);
        return Task.FromResult(ResultadoAlmacenamientoProveedor.Agregado);
    }
}
