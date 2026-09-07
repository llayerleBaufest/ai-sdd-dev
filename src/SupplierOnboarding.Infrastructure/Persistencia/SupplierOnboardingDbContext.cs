using Microsoft.EntityFrameworkCore;
using SupplierOnboarding.Domain.Proveedores;
using SupplierOnboarding.Infrastructure.Persistencia.Configuraciones;

namespace SupplierOnboarding.Infrastructure.Persistencia;

public sealed class SupplierOnboardingDbContext : DbContext
{
    public SupplierOnboardingDbContext(DbContextOptions<SupplierOnboardingDbContext> opciones)
        : base(opciones)
    {
    }

    public DbSet<Proveedor> Proveedores => Set<Proveedor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProveedorConfiguracion());

        base.OnModelCreating(modelBuilder);
    }
}
