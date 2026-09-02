# Guía de Validación Rápida: Registrar Proveedor

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Contrato**:
[contracts/registrar-proveedor.yaml](./contracts/registrar-proveedor.yaml)

Esta guía documenta cómo levantar el entorno local y ejecutar escenarios de validación
end-to-end de la funcionalidad. No sustituye a `tasks.md` (fase de implementación) ni contiene
código de producción.

## Prerrequisitos

- .NET 10 SDK instalado.
- Docker Desktop (o equivalente) en ejecución, requerido para:
  - Un contenedor local de SQL Server para desarrollo (opcional si ya se dispone de una instancia
    de SQL Server accesible).
  - Testcontainers, usado por `SupplierOnboarding.IntegrationTests` (ver
    [ADR-0006](../../docs/adr/ADR-0006-estrategia-pruebas-integracion.md)).

## Puesta en marcha local

1. Levantar SQL Server para desarrollo (ejemplo con contenedor local; no forma parte de recursos
   Azure ni de `rg-llayerle-ai-sdd-dev`, es solo para la máquina de desarrollo):

   ```powershell
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<contraseña-local-de-desarrollo>" `
     -p 1433:1433 --name sqlserver-dev -d mcr.microsoft.com/mssql/server:2022-latest
   ```

2. Configurar la cadena de conexión de desarrollo (por ejemplo, mediante `dotnet user-secrets` en
   `SupplierOnboarding.Api`, nunca en el repositorio, conforme al Principio II de la Constitution):

   ```powershell
   dotnet user-secrets set "ConnectionStrings:SupplierOnboarding" `
     "Server=localhost;Database=SupplierOnboarding;User Id=sa;Password=<contraseña-local-de-desarrollo>;TrustServerCertificate=True" `
     --project src/SupplierOnboarding.Api
   ```

3. Aplicar las migraciones de EF Core:

   ```powershell
   dotnet ef database update --project src/SupplierOnboarding.Infrastructure --startup-project src/SupplierOnboarding.Api
   ```

4. Ejecutar la API:

   ```powershell
   dotnet run --project src/SupplierOnboarding.Api
   ```

## Escenarios de validación (trazables a spec.md)

### Escenario 1 — Registro exitoso (Historia de Usuario 1)

```powershell
curl -X POST https://localhost:5001/api/proveedores `
  -H "Content-Type: application/json" `
  -d '{
        "razonSocial": "Acme S.A.",
        "pais": "AR",
        "identificadorFiscal": "30-12345678-9",
        "nombreContacto": "Jane Doe",
        "correoContacto": "jane.doe@acme.com"
      }'
```

**Resultado esperado**: `201 Created`, cuerpo con `id` asignado y `estado: "Pendiente"`, junto con
`registradoPor` y `registradoEn` (FR-012, FR-014, FR-015, FR-016). No se requiere una consulta
posterior: la propia respuesta del registro ya incluye identidad, estado y auditoría.

### Escenario 2 — Rechazo por datos obligatorios inválidos (Historia de Usuario 2)

```powershell
curl -X POST https://localhost:5001/api/proveedores `
  -H "Content-Type: application/json" `
  -d '{
        "razonSocial": "   ",
        "pais": "ZZ",
        "identificadorFiscal": "",
        "nombreContacto": "",
        "correoContacto": "no-es-un-correo"
      }'
```

**Resultado esperado**: `400 Bad Request` con un arreglo `errores` que incluye los cinco campos
inválidos simultáneamente (Caso Límite de `spec.md`).

### Escenario 3 — Rechazo por proveedor duplicado (Historia de Usuario 3)

Repetir el Escenario 1 con exactamente los mismos `pais`/`identificadorFiscal`, y luego repetirlo
nuevamente con una variante de formato irrelevante del identificador fiscal (por ejemplo,
`"30 12345678 9"` en minúsculas/mayúsculas distintas o con separadores distintos).

**Resultado esperado en ambos casos**: `409 Conflict` con un mensaje que solo informa la
existencia previa, sin exponer identidad interna, razón social ni estado del proveedor existente
(FR-018).

## Ejecución de pruebas automatizadas

```powershell
# Pruebas unitarias (dominio y caso de uso, sin infraestructura real)
dotnet test tests/SupplierOnboarding.UnitTests

# Pruebas de integración (requieren Docker en ejecución para Testcontainers)
dotnet test tests/SupplierOnboarding.IntegrationTests
```

## Trazabilidad de criterios de éxito

- SC-001, SC-002, SC-003: cubiertos por los Escenarios 1, 2 y 3 anteriores.
- SC-004, SC-005: verificables directamente en el cuerpo de la respuesta `201` del Escenario 1,
  sin necesidad de una consulta posterior.
- SC-006: verificable registrando dos proveedores en países distintos con el mismo valor textual
  de identificador fiscal y confirmando que ambos registros son `201 Created`.
