using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupplierOnboarding.Domain.Proveedores;

namespace SupplierOnboarding.Infrastructure.Persistencia.Configuraciones;

public sealed class ProveedorConfiguracion : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("Proveedores");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.RazonSocial)
            .IsRequired();

        builder.Property(p => p.Pais)
            .IsRequired();

        builder.Property(p => p.IdentificadorFiscal)
            .HasColumnName("IdentificadorFiscal")
            .IsRequired();

        builder.Property(p => p.IdentificadorFiscalNormalizado)
            .HasConversion(
                valor => valor.Valor,
                valor => IdentificadorFiscalNormalizado.Normalizar(valor))
            .HasColumnName("IdentificadorFiscalNormalizado")
            .IsRequired();

        builder.Property(p => p.NombreContacto)
            .IsRequired();

        builder.Property(p => p.CorreoContacto)
            .IsRequired();

        builder.Property(p => p.Estado)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.RegistradoPor)
            .IsRequired();

        builder.Property(p => p.RegistradoEn)
            .IsRequired();

        // Restricción de persistencia que refuerza FR-007/FR-008 ante condiciones de carrera (ADR-0005).
        builder.HasIndex(p => new { p.Pais, p.IdentificadorFiscalNormalizado })
            .IsUnique();
    }
}
