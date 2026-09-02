# ADR-0002: Separación de proyectos por responsabilidad

**Estado**: Aceptada
**Fecha**: 2026-09-02
**Funcionalidad de origen**: [001-registrar-proveedor](../../specs/001-registrar-proveedor/spec.md)

## Contexto / Problema

Dentro de la aplicación única decidida en ADR-0001, se debe decidir cómo organizar el código para
separar las reglas de dominio, el caso de uso `RegistrarProveedor`, los detalles técnicos de
persistencia y la entrada HTTP, de forma que el dominio y los casos de uso puedan probarse sin
infraestructura real (Principio VI y VII de la Constitution).

## Alternativas Consideradas

1. **Proyecto único** con carpetas internas (`Domain/`, `Application/`, `Infrastructure/`, `Api/`)
   sin límites de compilación entre ellas.
2. **Cuatro proyectos de producción** (`SupplierOnboarding.Domain`,
   `SupplierOnboarding.Application`, `SupplierOnboarding.Infrastructure`,
   `SupplierOnboarding.Api`) con referencias unidireccionales, más dos proyectos de pruebas
   (`SupplierOnboarding.UnitTests`, `SupplierOnboarding.IntegrationTests`).
3. **Feature folders / vertical slices** sin separar por capa técnica, organizando todo por
   funcionalidad de negocio dentro de un único proyecto.

## Decisión

Se adopta la alternativa 2, con la estructura de proyectos solicitada explícitamente para esta
funcionalidad: `SupplierOnboarding.Domain`, `SupplierOnboarding.Application`,
`SupplierOnboarding.Infrastructure`, `SupplierOnboarding.Api` en `src/`, y
`SupplierOnboarding.UnitTests`, `SupplierOnboarding.IntegrationTests` en `tests/`.

## Justificación

- Un límite de compilación real (proyectos separados, no solo carpetas) impide, de forma
  verificable por el compilador, que `Domain` y `Application` referencien accidentalmente EF Core,
  ASP.NET Core o cualquier paquete de infraestructura concreta, sosteniendo el Principio VII
  (Repository Pattern) y el Principio VI (Testabilidad como Requisito de Diseño).
- Separar `Application` de `Api` permite que el caso de uso `RegistrarProveedor` se pruebe de forma
  unitaria, rápida y determinística, sin levantar un servidor HTTP.
- Separar `Infrastructure` de `Domain`/`Application` permite cambiar la tecnología de persistencia
  en el futuro sin tocar reglas de negocio, y confina la traducción de excepciones específicas de
  EF Core/SQL Server (ver ADR-0005) a un único lugar.
- Los dos proyectos de pruebas reflejan la distinción explícita exigida por la restricción de
  Testing de esta funcionalidad: unitarias (sin infraestructura real) vs. integración (con SQL
  Server real vía Testcontainers, ver ADR-0006).
- Esta separación **no** se justifica como aplicación mecánica de "Clean Architecture", sino por la
  necesidad concreta de aislar reglas de dominio, casos de uso, infraestructura y entrada HTTP,
  exigida explícitamente por el alcance de esta planificación y por los Principios VI y VII de la
  Constitution.

## Trade-offs

- Más proyectos que una única solución simple: mayor cantidad de archivos `.csproj`, referencias
  entre proyectos y una curva de navegación ligeramente mayor.
- Cambios que crucen varias capas (por ejemplo, agregar un campo nuevo al proveedor) requieren
  tocar más de un proyecto.
- Se acepta este costo porque el beneficio de aislamiento y testabilidad es directamente exigido
  por la Constitution y por la restricción explícita de este plan, y el número de proyectos se
  mantiene acotado (4 + 2), sin fragmentación adicional no solicitada.
