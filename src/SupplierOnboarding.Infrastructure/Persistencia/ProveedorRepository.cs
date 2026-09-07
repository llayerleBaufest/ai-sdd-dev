using Microsoft.EntityFrameworkCore;
using SupplierOnboarding.Domain.Proveedores;

namespace SupplierOnboarding.Infrastructure.Persistencia;

public sealed class ProveedorRepository : IProveedorRepository
{
    private readonly SupplierOnboardingDbContext _dbContext;

    public ProveedorRepository(SupplierOnboardingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExisteAsync(string pais, string identificadorFiscalNormalizado, CancellationToken cancellationToken)
    {
        // No se puede traducir a SQL el acceso a ".Valor" sobre la propiedad convertida
        // (IdentificadorFiscalNormalizado); se compara contra una instancia del value object.
        var identificadorNormalizado = IdentificadorFiscalNormalizado.Normalizar(identificadorFiscalNormalizado);

        return _dbContext.Proveedores
            .AnyAsync(
                p => p.Pais == pais && p.IdentificadorFiscalNormalizado == identificadorNormalizado,
                cancellationToken);
    }

    public async Task<ResultadoAlmacenamientoProveedor> AgregarAsync(Proveedor proveedor, CancellationToken cancellationToken)
    {
        _dbContext.Proveedores.Add(proveedor);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ResultadoAlmacenamientoProveedor.Agregado;
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(proveedor).State = EntityState.Detached;
            return ResultadoAlmacenamientoProveedor.ConflictoDuplicado;
        }
    }
}
