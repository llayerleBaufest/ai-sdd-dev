# ADR-0008: Estrategia inicial de observabilidad

**Estado**: Aceptada
**Fecha**: 2026-09-02
**Funcionalidad de origen**: [001-registrar-proveedor](../../specs/001-registrar-proveedor/spec.md)

## Contexto / Problema

El Principio III de la Constitution exige que la observabilidad (logging estructurado,
correlación, métricas y trazas distribuidas) forme parte del diseño inicial, no que se agregue
después. Este plan indica explícitamente que aún no es necesario incorporar Azure Application
Insights porque no existe un despliegue cloud en esta fase. Se debe decidir el mecanismo concreto de
observabilidad a preparar desde ahora.

## Alternativas Consideradas

1. **Solo `ILogger` básico**, sin correlación estructurada ni trazas/métricas.
2. **OpenTelemetry .NET** (`OpenTelemetry.Extensions.Hosting`,
   `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.EntityFrameworkCore`,
   `OpenTelemetry.Instrumentation.SqlClient`), con logging estructurado vía `ILogger` correlacionado
   automáticamente por `Activity`/contexto de traza, y un exportador de consola/OTLP local en esta
   fase.
3. **Serilog + Azure Application Insights** desde el inicio.

## Decisión

Se adopta la alternativa 2: configurar **OpenTelemetry .NET** en `SupplierOnboarding.Api` para
trazas (instrumentación de ASP.NET Core, EF Core y `SqlClient`) y métricas, con logging estructurado
a través de `ILogger` (que hereda automáticamente el contexto de correlación de `Activity` de
OpenTelemetry), exportando a consola/OTLP local en esta fase.

## Justificación

- OpenTelemetry es el estándar abierto explícitamente solicitado por este plan y por el Principio
  III de la Constitution ("se deben preferir mecanismos compatibles con OpenTelemetry").
- Configurar la instrumentación desde `Program.cs` en `Api` (capa de composición) permite cambiar el
  exportador (por ejemplo, a Azure Monitor/Application Insights cuando exista un despliegue cloud
  real) **sin modificar el dominio, los casos de uso ni ninguna regla de negocio**, cumpliendo la
  restricción de que el diseño debe permitir conectar observabilidad posteriormente sin tocar
  reglas de negocio.
- No se incorpora Azure Application Insights todavía porque no existe un despliegue cloud en esta
  fase, tal como indica explícitamente este plan; agregarlo ahora violaría el Principio V
  (Simplicidad antes que Distribución) y la restricción de no crear/depender de recursos Azure sin
  necesidad concreta.
- Se descarta Serilog como pieza adicional porque `ILogger` + OpenTelemetry Logs ya cubren logging
  estructurado y correlación sin sumar una biblioteca de logging adicional; si en el futuro se
  requiere un sink específico no cubierto por el exportador OTLP, esa necesidad deberá justificarse
  explícitamente.

## Trade-offs

- El exportador de consola/OTLP local no persiste datos útiles para operación productiva real; esto
  es aceptable porque el objetivo actual es preparar el diseño para observabilidad, no operar
  observabilidad productiva (no hay despliegue cloud todavía).
- Instrumentar EF Core y `SqlClient` añade una dependencia adicional de paquetes de instrumentación
  de OpenTelemetry; se acepta porque son paquetes oficiales del proyecto OpenTelemetry .NET,
  mantenidos activamente y alineados con la restricción explícita de este plan.
- Cuando exista un despliegue cloud real, la incorporación de Azure Monitor como exportador deberá
  evaluarse y documentarse (posible ADR de reemplazo o de extensión de esta), sin que ello implique
  cambios en el código de dominio o de aplicación.
