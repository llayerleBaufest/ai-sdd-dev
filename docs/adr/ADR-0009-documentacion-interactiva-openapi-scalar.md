# ADR-0009: Documentación interactiva de la API (OpenAPI nativo + Scalar)

**Estado**: Aceptada
**Fecha**: 2026-09-03
**Funcionalidad de origen**: [001-registrar-proveedor](../../specs/001-registrar-proveedor/spec.md)

## Contexto / Problema

Se necesita una capacidad de prueba manual y documentación interactiva del endpoint
`POST /api/proveedores` en entorno `Development`, para agilizar la verificación exploratoria
durante el desarrollo. Esta necesidad es de Developer Experience: no está motivada por ningún
requisito funcional de `spec.md`, no debe modificarlo, y debe resolverse sin incorporar tecnología
ni recursos Azure adicionales sin una razón concreta (Principio VIII), ni ampliar la superficie de
ataque en producción por defecto (Principio II).

## Alternativas Consideradas

1. **Swashbuckle.AspNetCore**: generación del documento OpenAPI mediante reflection/filtros propios
   de la librería, junto con Swagger UI como interfaz interactiva.
2. **Soporte nativo de OpenAPI de ASP.NET Core en .NET 10** (`Microsoft.AspNetCore.OpenApi`,
   `AddOpenApi()` + `MapOpenApi()`) para generar el documento, combinado con **Scalar.AspNetCore**
   exclusivamente como UI interactiva de desarrollo.
3. **Ninguna documentación interactiva**: mantener únicamente el contrato estático
   `contracts/registrar-proveedor.yaml`.

## Decisión

Se adopta la alternativa 2. El documento OpenAPI se genera con el soporte nativo de .NET 10
(`builder.Services.AddOpenApi()` + `app.MapOpenApi()`), sin Swashbuckle. `Scalar.AspNetCore` se
agrega únicamente como UI interactiva de prueba manual, mapeada exclusivamente dentro de un bloque
`if (app.Environment.IsDevelopment())`; no queda habilitada por defecto en producción. Se agrega
además un archivo `.http` versionado con ejemplos de requests para "Registrar proveedor" como
complemento reproducible bajo control de versiones.

## Justificación

- .NET 10 incluye soporte nativo para generar el documento OpenAPI sin depender de Swashbuckle; no
  existe una razón concreta para incorporar una dependencia adicional que el soporte nativo ya
  cubre para esta funcionalidad (Principio VIII: no incorporar tecnología "porque sea habitual").
- Scalar.AspNetCore se usa exclusivamente como capa de presentación (UI) sobre el documento OpenAPI
  estándar generado por `MapOpenApi()`; no sustituye ni duplica la generación del documento, y no
  acopla lógica de negocio a esa librería.
- Restringir Scalar a `Development` evita ampliar la superficie de exploración/prueba de la API en
  producción sin necesidad de negocio, cumpliendo el Principio II (Seguridad por Defecto).
- El archivo `.http` versionado complementa la UI interactiva con ejemplos reproducibles de
  requests (éxito, error de validación, duplicado) sin depender de herramientas externas (por
  ejemplo Postman) ni de estado no versionado.
- Es una decisión de Developer Experience: no modifica el comportamiento observable definido en
  `spec.md`, ni agrega endpoints, entidades o reglas de negocio nuevas.

## Trade-offs

- Si en el futuro surge un requisito de exponer documentación pública de la API para consumidores
  externos (versionado publicado, control de acceso propio, etc.), esta decisión debe revisarse
  explícitamente (posible ADR de reemplazo), evaluando entonces autenticación de la UI u otras
  opciones de publicación.
- Scalar.AspNetCore es una dependencia de terceros (no Microsoft); se acepta el riesgo porque su
  alcance se limita a una UI de desarrollo, gateada a `Development`, sin participar en el
  comportamiento de producción.
