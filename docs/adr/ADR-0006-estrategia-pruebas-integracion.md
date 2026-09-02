# ADR-0006: Estrategia de pruebas de integración

**Estado**: Aceptada
**Fecha**: 2026-09-02
**Funcionalidad de origen**: [001-registrar-proveedor](../../specs/001-registrar-proveedor/spec.md)

## Contexto / Problema

Se debe decidir cómo probar, de forma realista y reproducible, el comportamiento de EF Core, la
implementación real de `IProveedorRepository`, la restricción de unicidad (ADR-0005) y el pipeline
HTTP de `SupplierOnboarding.Api`, sin depender de instalaciones locales frágiles ni de un proveedor
de base de datos con semántica distinta a la de producción.

## Alternativas Consideradas

1. **Proveedor EF Core InMemory** para las pruebas de integración.
2. **SQL Server LocalDB** instalado localmente en cada máquina de desarrollo y en el agente de CI.
3. **Testcontainers** (`Testcontainers.MsSql`), levantando un contenedor efímero de SQL Server real
   por ejecución de pruebas.

## Decisión

Se adopta la alternativa 3: **Testcontainers** con la imagen oficial de SQL Server, orquestado
desde `SupplierOnboarding.IntegrationTests` (por ejemplo, mediante un fixture de colección de xUnit
que levanta el contenedor una vez por ejecución de la suite y aplica las migraciones de EF Core
antes de las pruebas).

## Justificación

- El proveedor InMemory de EF Core **no aplica restricciones de unicidad a nivel de índice** ni
  reproduce el comportamiento transaccional ni los códigos de error reales de SQL Server; no sirve
  para validar la característica central de ADR-0005 (unicidad garantizada por índice único y
  traducción de la excepción real de conflicto).
- La restricción explícita de este plan indica preferir pruebas contra SQL Server real o efímero
  frente a un proveedor con semántica diferente cuando la característica bajo prueba depende del
  comportamiento de SQL Server; la unicidad concurrente es exactamente ese caso.
- Frente a LocalDB, Testcontainers no depende de una instalación previa en la máquina de desarrollo
  ni en el agente de CI (solo requiere Docker), es reproducible entre entornos y se descarta
  automáticamente al finalizar la ejecución, evitando estado residual entre corridas de pruebas.

## Trade-offs

- Requiere Docker disponible en el entorno de desarrollo y en el pipeline de CI; si algún entorno
  no dispone de Docker, esta suite de pruebas no podrá ejecutarse allí (se documenta como
  prerequisito en `quickstart.md`).
- El tiempo de ejecución de las pruebas de integración es mayor que con InMemory, debido al arranque
  del contenedor; se acepta porque esta suite es acotada (persistencia, unicidad, HTTP) y no
  reemplaza a las pruebas unitarias, que permanecen rápidas y sin infraestructura real.
- Se pospone la evaluación de LocalDB o de una instancia compartida de SQL Server como alternativa
  únicamente si Testcontainers resultara inviable en algún entorno de CI futuro; de ocurrir, esa
  reconsideración debe registrarse en una nueva ADR.
