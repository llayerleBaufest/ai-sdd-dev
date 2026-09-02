# ADR-0003: Mecanismo de persistencia (EF Core 10 + SQL Server)

**Estado**: Aceptada
**Fecha**: 2026-09-02
**Funcionalidad de origen**: [001-registrar-proveedor](../../specs/001-registrar-proveedor/spec.md)

## Contexto / Problema

Se debe elegir la tecnología de acceso a datos para persistir proveedores, respetando el Repository
Pattern obligatorio (Principio VII de la Constitution) y la plataforma exigida (.NET 10, Azure).

## Alternativas Consideradas

1. **Entity Framework Core 10** sobre SQL Server (desarrollo/integración) con Azure SQL Database
   como destino cloud futuro.
2. **Dapper** (micro-ORM) sobre SQL Server, con SQL manual para cada operación del repositorio.
3. Otro ORM o motor de base de datos distinto de SQL Server/Azure SQL.

## Decisión

Se adopta la alternativa 1: **Entity Framework Core 10**, confinado al proyecto
`SupplierOnboarding.Infrastructure`, sobre **SQL Server** como motor de desarrollo e integración,
con **Azure SQL Database** como destino cloud previsto (sin crearse en esta fase).

## Justificación

- La plataforma exigida para este plan es explícitamente .NET 10 + EF Core 10 + SQL Server /
  Azure SQL Database; no se evalúan motores fuera de esa restricción explícita.
- EF Core provee migraciones versionadas, mapeo objeto-relacional productivo y soporte nativo para
  expresar restricciones de unicidad a nivel de índice (necesario para ADR-0005), reduciendo
  código repetitivo frente a SQL manual.
- Confinar EF Core a `Infrastructure` y exponerlo únicamente a través de `IProveedorRepository`
  cumple el Principio VII: `Domain` y `Application` no dependen de `DbContext`, `DbSet` ni SQL.

## Trade-offs

- EF Core introduce una capa de abstracción con comportamiento menos explícito que SQL manual
  (por ejemplo, generación de consultas, tracking de cambios); se mitiga manteniendo el
  repositorio con operaciones acotadas y explícitas (no genéricas) y verificando el comportamiento
  real mediante pruebas de integración contra SQL Server real (ADR-0006).
- Frente a Dapper, EF Core es más pesado en tiempo de arranque y superficie de API, pero se
  prefiere por la productividad en migraciones y porque el proyecto ya requiere EF Core 10 como
  restricción de plataforma.
- Azure SQL Database no se crea todavía; su incorporación real como destino de despliegue queda
  pendiente de una fase de infraestructura posterior, evaluando entonces Bicep como mecanismo de
  Infrastructure as Code.
