# Investigación (Fase 0): Registrar Proveedor

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

Este documento resuelve las decisiones técnicas necesarias para planificar "Registrar proveedor".
Las decisiones significativas se registran como ADRs en [docs/adr/](../../docs/adr/); aquí se
consolidan en el formato Decisión / Justificación / Alternativas consideradas exigido por el flujo
de planificación.

## 1. Aplicación única frente a microservicios

- **Decisión**: monolito modular, una única aplicación desplegable (`SupplierOnboarding.Api`).
- **Justificación**: no existe ningún requisito de escalabilidad independiente, ownership separado
  ni despliegue independiente que justifique la complejidad distribuida (Principio V de la
  Constitution).
- **Alternativas consideradas**: microservicios por bounded context; monolito no estructurado.
- **Detalle completo**: [ADR-0001](../../docs/adr/ADR-0001-aplicacion-unica-vs-microservicios.md).

## 2. Separación de proyectos

- **Decisión**: 4 proyectos de producción (Domain, Application, Infrastructure, Api) + 2 de
  pruebas (UnitTests, IntegrationTests), con dependencias unidireccionales hacia Domain.
- **Justificación**: aísla reglas de dominio y casos de uso de tecnologías concretas, permitiendo
  pruebas unitarias sin infraestructura real (Principios VI y VII); no se justifica como aplicación
  mecánica de "Clean Architecture" sino por la necesidad funcional de separar responsabilidades.
- **Alternativas consideradas**: proyecto único con carpetas; feature folders/vertical slices.
- **Detalle completo**: [ADR-0002](../../docs/adr/ADR-0002-separacion-de-proyectos.md).

## 3. Mecanismo de persistencia

- **Decisión**: Entity Framework Core 10 sobre SQL Server (dev/integración), con Azure SQL
  Database como destino cloud futuro (no creado en esta fase).
- **Justificación**: cumple la plataforma exigida; provee migraciones y soporte nativo de índices
  únicos, necesario para la estrategia de unicidad (ítem 5).
- **Alternativas consideradas**: Dapper + SQL manual; otro motor de base de datos.
- **Detalle completo**: [ADR-0003](../../docs/adr/ADR-0003-mecanismo-persistencia.md).

## 4. Formato técnico del identificador interno (FR-014)

- **Decisión**: GUID versión 7 (`Guid.CreateVersion7(instante)`, nativo en .NET 10), generado por
  el dominio al crear el `Proveedor`, recibiendo el instante de registro como parámetro (ver punto
  10) en lugar de leer un reloj global.
- **Justificación**: unicidad global sin coordinación central, generación posible en el dominio sin
  necesidad de un round-trip a la base de datos (compatible con Repository Pattern y pruebas
  unitarias sin infraestructura real) y orden temporal aproximado que reduce la fragmentación de
  índice frente a un GUID v4 puramente aleatorio, sin dependencias externas. Este formato **no**
  cumple ninguna función de seguridad ni autorización: ninguna regla de negocio o control de acceso
  depende de que el identificador sea difícil de predecir (FR-014 solo exige que sea propio y
  único). Recibir el instante como parámetro permite generar el mismo GUID de forma determinística
  en pruebas.
- **Alternativas consideradas**: entero autoincremental (descartado porque exige asignación por la
  base de datos, dificultando construir y probar el agregado antes de persistir); GUID v4 aleatorio;
  ULID de terceros.
- **Detalle completo**: [ADR-0004](../../docs/adr/ADR-0004-formato-identificador-interno.md).

## 5. Estrategia de unicidad país + identificador fiscal (FR-007, FR-008, FR-010)

- **Decisión**: la unicidad de `(Pais, IdentificadorFiscalNormalizado)` se trata como una regla de
  negocio *cross-aggregate*, no como una invariante que la entidad `Proveedor` pueda verificar por
  sí misma. `Application` la verifica mediante `IProveedorRepository.ExisteAsync` (mensaje de
  negocio claro en el camino feliz), **combinada** con un índice `UNIQUE` en base de datos sobre
  `(Pais, IdentificadorFiscalNormalizado)`; la violación de dicho índice se traduce, en
  `Infrastructure`, a un resultado funcional (`ConflictoDuplicado`) — no a una excepción de
  dominio — que `Application` interpreta con el mismo resultado observable que la verificación
  previa: rechazo por duplicado.
- **Justificación**: la verificación previa por sí sola no es segura ante condiciones de carrera
  (Caso Límite de concurrencia de `spec.md`); el índice único en base de datos es la única garantía
  atómica real. Representar el conflicto como un resultado esperado del caso de uso (en lugar de
  una excepción) evita usar excepciones para control de flujo de un desenlace de negocio habitual
  (FR-009), y evita ubicar en `Domain` una excepción de duplicado que la entidad aislada no puede
  detectar por sí misma.
- **Alternativas consideradas**: verificación previa únicamente; bloqueo pesimista/`SERIALIZABLE`;
  excepción de dominio (`ProveedorDuplicadoException`) en lugar de un resultado funcional.
- **Detalle completo**: [ADR-0005](../../docs/adr/ADR-0005-estrategia-unicidad.md).

## 6. Estrategia de pruebas de integración

- **Decisión**: Testcontainers (`Testcontainers.MsSql`) para levantar un SQL Server real y efímero
  durante la ejecución de `SupplierOnboarding.IntegrationTests`.
- **Justificación**: el proveedor EF Core InMemory no aplica restricciones de unicidad ni refleja
  el comportamiento transaccional real de SQL Server, necesario para validar el ítem 5; Testcontainers
  no requiere instalaciones locales de LocalDB y es reproducible en CI.
- **Alternativas consideradas**: EF Core InMemory; SQL Server LocalDB instalado localmente.
- **Detalle completo**: [ADR-0006](../../docs/adr/ADR-0006-estrategia-pruebas-integracion.md).

## 7. Decisión sobre FluentValidation

- **Decisión**: no se incorpora FluentValidation; se implementa un validador manual y explícito
  (`RegistrarProveedorValidador`) en `Application` que acumula todos los errores de FR-002 a
  FR-006.
- **Justificación**: las reglas de entrada son simples y sin condicionales cruzados; una lista de
  errores acumulada manualmente satisface igual de bien el Caso Límite de múltiples errores
  simultáneos, sin sumar una dependencia sin necesidad concreta (restricción explícita del plan y
  Principio VIII).
- **Alternativas consideradas**: FluentValidation con `AbstractValidator`.
- **Detalle completo**: [ADR-0007](../../docs/adr/ADR-0007-decision-fluentvalidation.md).

## 8. Estrategia inicial de observabilidad

- **Decisión**: OpenTelemetry .NET (trazas + métricas, instrumentación de ASP.NET Core, EF Core y
  SqlClient) configurado en `Api`, con logging estructurado vía `ILogger` correlacionado por
  `Activity`, exportando a consola/OTLP local en esta fase; sin Azure Application Insights todavía.
- **Justificación**: cumple el Principio III (observabilidad por defecto) sin requerir un
  despliegue cloud; permite conectar un exportador de Azure Monitor más adelante sin tocar reglas
  de negocio.
- **Alternativas consideradas**: solo `ILogger` básico; Serilog + Application Insights desde el
  inicio.
- **Detalle completo**: [ADR-0008](../../docs/adr/ADR-0008-estrategia-observabilidad.md).

## 9. Dobles de prueba para pruebas unitarias (nota de diseño, no ADR)

- **Decisión**: para las pruebas unitarias de `RegistrarProveedorCasoDeUso`, se implementa un fake
  simple de `IProveedorRepository` (clase en memoria dentro de `SupplierOnboarding.UnitTests`) en
  lugar de incorporar una biblioteca de mocking (por ejemplo Moq o NSubstitute).
- **Justificación**: `IProveedorRepository` expone únicamente dos operaciones (`ExisteAsync`,
  `AgregarAsync`); un fake en memoria es más simple de leer y mantener que configurar una
  biblioteca de mocking para una interfaz tan pequeña, cumpliendo la instrucción de "evitar mocks
  innecesariamente complejos". Si en el futuro el repositorio u otras dependencias del caso de uso
  crecen en complejidad de comportamiento a simular, se reevaluará la incorporación de una
  biblioteca de mocking.

## 10. Abstracción de tiempo para la creación de proveedores

- **Decisión**: `RegistrarProveedorCasoDeUso` (Application) obtiene el instante de registro
  mediante `TimeProvider` (abstracción nativa de .NET, inyectada por DI; `TimeProvider.System` en
  producción) y lo pasa explícitamente al crear el `Proveedor`. `Domain` no invoca
  `DateTimeOffset.UtcNow`, `DateTime.Now` ni ningún otro reloj global.
- **Justificación**: permite pruebas unitarias determinísticas de `RegistrarProveedorCasoDeUso` y
  de `Proveedor` (mediante `FakeTimeProvider` o un instante fijo pasado directamente al dominio),
  sin introducir una dependencia externa: `TimeProvider` es parte de .NET desde la versión 8.
- **Alternativas consideradas**: `DateTimeOffset.UtcNow` directo en Domain (descartado, impide
  pruebas determinísticas); una interfaz de reloj propia (`IRelojDelSistema`) (descartada, no
  aporta nada sobre `TimeProvider` ya provisto por el framework).
- No amerita un ADR independiente: es el uso directo de una abstracción estándar de .NET sin
  alternativas arquitectónicas en disputa.

## 11. Abstracción de identidad del usuario actual para `RegistradoPor` (corrección U1)

- **Decisión**: se define un puerto propio y mínimo `IUsuarioActual` en `Application`
  (`src/SupplierOnboarding.Application/Identidad/IUsuarioActual.cs`), que expone únicamente el
  identificador del usuario autenticado (por ejemplo `string IdentificadorUsuario { get; }`), sin
  ninguna dependencia de ASP.NET Core, `ClaimsPrincipal` ni `HttpContext`. `RegistrarProveedorCasoDeUso`
  obtiene `RegistradoPor` exclusivamente mediante esta dependencia inyectada, nunca desde
  `RegistrarProveedorComando` ni desde el body de `POST /api/proveedores`. `Api` provee una
  implementación temporal (`UsuarioActualHttp`) que lee el identificador del `ClaimsPrincipal`/
  `HttpContext` actual, sin implementar todavía Microsoft Entra ID ni ningún otro proveedor
  concreto de autenticación (FR-019 continúa fuera de alcance). Las pruebas unitarias usan un fake
  (`UsuarioActualFake`) que permite fijar explícitamente el identificador de usuario.
- **Justificación**: sin esta abstracción, el cliente HTTP podría proporcionar o manipular
  libremente `RegistradoPor` en el body de la solicitud, lo cual contradice FR-015 (auditoría
  confiable de quién registró el proveedor) y el Principio II de la Constitution (operaciones
  sensibles auditables). Definir el puerto en `Application` (no en `Domain`, que no conoce el
  concepto de "usuario actual"; ni en `Api`, para no acoplar el caso de uso a ASP.NET Core) sigue
  el mismo patrón de inversión de dependencias ya usado para `IProveedorRepository` (Principio VI)
  y deja preparada la integración futura de un proveedor de autenticación real sin cambiar el caso
  de uso.
- **Alternativas consideradas**: aceptar `RegistradoPor` como campo del body HTTP (descartado: el
  cliente podría suplantar a cualquier usuario, violando el Principio II); leer directamente
  `HttpContext.User` dentro de `RegistrarProveedorCasoDeUso` (descartado: acopla `Application` a
  ASP.NET Core y hace imposible probar el caso de uso sin infraestructura web, violando el
  Principio VI); posponer la decisión hasta implementar autenticación real (descartado: dejaría
  `RegistradoPor` sin una fuente definida y no controlable en pruebas, tal como señaló el hallazgo
  U1 de `/speckit-analyze`).
- No amerita un ADR independiente: es la misma aplicación del Principio VI (interfaces para
  testabilidad/desacoplamiento) ya usada para `IProveedorRepository`, sin una alternativa
  arquitectónica en disputa; no introduce microservicios, mensajería ni un proveedor de
  autenticación concreto.

## Resolución de "NEEDS CLARIFICATION"

No quedan elementos marcados como `NEEDS CLARIFICATION` en el Contexto Técnico de `plan.md`. Los
objetivos de rendimiento no cuantificados (SC-001, "confirmación inmediata") se documentan como un
gap ya identificado en el checklist de calidad (CHK018) y no se resuelven aquí inventando un
umbral no aprobado por el negocio; este plan no depende de dicho umbral para ninguna decisión
técnica.
