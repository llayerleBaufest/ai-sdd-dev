# Plan de Implementación: Registrar Proveedor

**Rama**: `001-registrar-proveedor` | **Fecha**: 2026-09-02 | **Spec**: [spec.md](./spec.md)

**Entrada**: Especificación de funcionalidad desde `/specs/001-registrar-proveedor/spec.md`

**Nota**: Este plan es generado por el comando `/speckit-plan`. Todas las decisiones aquí
documentadas respetan los requisitos funcionales vigentes en `spec.md`; no se modifican ni se
inventan reglas de negocio nuevas.

## Resumen

Implementar la funcionalidad "Registrar proveedor" como una única aplicación desplegable (monolito
modular) en .NET 10 / ASP.NET Core, expuesta mediante Minimal APIs. El comportamiento observable se
limita a lo definido por la especificación: registrar un proveedor validando sus cinco datos
obligatorios, asignándole una identidad propia y dejándolo en estado "Pendiente", rechazando datos
inválidos o combinaciones duplicadas de país + identificador fiscal normalizado. Quién registra
(`RegistradoPor`) se obtiene siempre de la identidad autenticada del usuario mediante una pequeña
abstracción (`IUsuarioActual`), nunca de un valor provisto por el cliente HTTP; la
autenticación/autorización concretas continúan fuera de alcance (FR-019), pero la arquitectura
queda preparada para integrarlas sin cambiar el caso de uso. La solución se
organiza en cuatro proyectos de producción (Domain, Application, Infrastructure, Api) que separan
reglas de dominio, casos de uso, detalles técnicos de persistencia y entrada HTTP, más dos
proyectos de pruebas (UnitTests, IntegrationTests). La persistencia usa Entity Framework Core 10
sobre SQL Server (Azure SQL Database como destino cloud futuro), con Repository Pattern específico
de dominio y una restricción de unicidad garantizada a nivel de base de datos para resistir
condiciones de carrera. No se incorporan microservicios, mensajería, Microsoft Foundry ni otros
servicios Azure adicionales, por ausencia de un requisito o atributo de calidad que lo justifique
(Principio V de la Constitution). Las decisiones arquitectónicas significativas se documentan como
ADRs en [docs/adr/](../../docs/adr/).

## Contexto Técnico

**Lenguaje/Versión**: C# 14 sobre .NET 10.

**Dependencias Principales**: ASP.NET Core (Minimal APIs), Entity Framework Core 10 (proveedor SQL
Server), OpenTelemetry .NET (`OpenTelemetry.Extensions.Hosting`,
`OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.EntityFrameworkCore`),
xUnit, `TimeProvider` (abstracción nativa de .NET, sin paquete adicional, usada en Application para
obtener el instante de registro sin que Domain dependa de un reloj global). También se define
`IUsuarioActual` (abstracción propia y mínima, sin paquete NuGet) en Application, implementada en
Api, para obtener el identificador del usuario autenticado que ejecuta el registro sin que
Application ni Domain dependan de ASP.NET Core, `ClaimsPrincipal` ni `HttpContext`. Ver
[research.md](./research.md) para las decisiones sobre FluentValidation (rechazada por ahora),
Testcontainers (adoptada solo para pruebas de integración) e `IUsuarioActual`.

**Almacenamiento**: SQL Server (instancia local o contenedor efímero para desarrollo e
integración). Azure SQL Database es el destino cloud previsto, sin crearse todavía (ver
[ADR-0003](../../docs/adr/ADR-0003-mecanismo-persistencia.md)).

**Testing**: xUnit para pruebas unitarias y de integración. Pruebas unitarias con dobles de prueba
(fakes) escritos a mano para `IProveedorRepository`, sin biblioteca de mocking adicional. Pruebas
de integración con Testcontainers (imagen de SQL Server) contra ASP.NET Core + EF Core reales (ver
[ADR-0006](../../docs/adr/ADR-0006-estrategia-pruebas-integracion.md)).

**Plataforma Objetivo**: Servicio HTTP auto-hospedado en Kestrel (Windows/Linux), contenerizable;
destino cloud futuro considerado Azure Container Apps (no se crea en esta fase; ver sección Azure).

**Tipo de Proyecto**: Servicio web único (backend HTTP), sin frontend en el alcance de esta
especificación.

**Objetivos de Rendimiento**: No cuantificados por la especificación. SC-001 exige "confirmación
inmediata del éxito" sin definir un umbral numérico (gap ya identificado en
[checklists/calidad.md](./checklists/calidad.md), ítem CHK018); este plan no inventa un umbral no
aprobado por el negocio. Se aplican prácticas estándar de rendimiento de ASP.NET Core sin metas
numéricas adicionales en esta fase.

**Restricciones**: Ninguna restricción numérica de latencia, memoria u operación offline está
definida por la especificación. La única restricción dura documentada es la de unicidad
país + identificador fiscal normalizado, incluso ante escritura concurrente (Caso Límite de la
especificación).

**Escala/Alcance**: Una entidad de dominio (Proveedor), un caso de uso (RegistrarProveedor) y un
único endpoint de escritura (`POST /api/proveedores`). La especificación no exige consultar
posteriormente un proveedor ya registrado: los escenarios 2 y 3 de la Historia de Usuario 1
(identidad, estado y auditoría verificables) quedan satisfechos con la propia respuesta del
registro (FR-014, FR-015), por lo que no se incorpora un endpoint de lectura
(`GET /api/proveedores/{id}`) sin un requisito explícito que lo justifique. No se agregan
endpoints, entidades ni operaciones adicionales fuera de este alcance.

## Constitution Check

*GATE: Debe cumplirse antes de la Fase 0 de investigación. Se reevalúa después del diseño de la
Fase 1.*

| Principio / Restricción | Evaluación | Cumple |
|---|---|---|
| I. Intención de Negocio y Trazabilidad Primero | Todas las decisiones de este plan derivan de FR-001 a FR-020 y de los Supuestos de `spec.md`; ninguna regla nueva se introduce. Las decisiones significativas quedan registradas como ADRs. | Sí |
| II. Seguridad por Defecto | Validación de entrada en el límite del sistema (capa Application, invocada desde Api), reforzada además por invariantes de guarda en el propio constructor de `Proveedor` en Domain; identificador interno GUID v7 (ver ADR-0004) elegido por unicidad, generación distribuida y orden temporal favorable para persistencia, sin que ninguna regla de seguridad dependa de que dicho identificador sea difícil de predecir; FR-019 documenta que autenticación/autorización quedan fuera de alcance (asumidas como capacidad existente); `RegistradoPor` se obtiene exclusivamente de la identidad autenticada mediante `IUsuarioActual` (Application), nunca de un campo editable por el cliente en el body HTTP, dejando preparada la integración futura con un proveedor de autenticación real sin implementarlo todavía; no se almacenan secretos en el repositorio (cadena de conexión vía configuración/entorno, Managed Identity previsto para Azure SQL futuro). | Sí |
| III. Testeabilidad y Observabilidad por Defecto | Separación explícita de pruebas unitarias (dominio/aplicación) e integración (infraestructura/HTTP); diseño preparado desde el inicio para logging estructurado, correlación, métricas y trazas mediante OpenTelemetry (ver ADR-0008), sin requerir Azure Application Insights todavía. | Sí |
| IV. Responsabilidad Humana sobre la IA | No aplica: esta funcionalidad no incorpora componentes de IA ni decisiones automatizadas de negocio. | N/A |
| V. Simplicidad antes que Distribución | Aplicación única (monolito modular), sin microservicios, sin mensajería asíncrona, sin servicios Azure adicionales; justificado en ADR-0001. | Sí |
| VI. Testabilidad como Requisito de Diseño | `IProveedorRepository` como abstracción para sustituir infraestructura real en pruebas unitarias mediante fakes simples; el instante de registro se obtiene en Application mediante `TimeProvider` (sin reloj global en Domain) y se pasa explícitamente al crear el `Proveedor`, permitiendo pruebas determinísticas; de igual modo, `IUsuarioActual` permite sustituir la identidad del usuario actual por un fake (`UsuarioActualFake`) en pruebas unitarias, sin depender de `ClaimsPrincipal`/`HttpContext`; el rechazo por duplicado se modela como un resultado esperado del caso de uso, no como una excepción de dominio (ver ADR-0005); no se crean interfaces mecánicas adicionales sin necesidad real. | Sí |
| VII. Acceso a Datos mediante Repository Pattern | `IProveedorRepository` específico de dominio (no genérico), con únicamente las operaciones que el caso de uso necesita, incluyendo un resultado que distingue inserción exitosa de conflicto por restricción única (sin exponer excepciones de EF Core/SQL Server fuera de Infrastructure); implementación concreta en Infrastructure; Domain/Application no referencian EF Core, DbContext ni SQL. | Sí |
| VIII. Calidad, Simplicidad y Buenas Prácticas de Código | La separación en 4 proyectos se justifica funcionalmente (dominio/casos de uso/infraestructura/HTTP), no como aplicación mecánica de "Clean Architecture"; se evita FluentValidation y mocks complejos cuando la validación manual y los fakes bastan (ver research.md). | Sí |
| Restricciones de Ingeniería y Entorno Azure | No se crean recursos Azure durante la planificación; cualquier recurso futuro pertenecerá exclusivamente a `rg-llayerle-ai-sdd-dev`; no se incorporan Microsoft Foundry, Service Bus, Functions, Document Intelligence u otros servicios no requeridos por esta funcionalidad. | Sí |

**Resultado**: Sin violaciones. No se requiere completar la tabla de Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/001-registrar-proveedor/
├── plan.md              # Este archivo (salida del comando /speckit-plan)
├── research.md          # Salida de la Fase 0 (/speckit-plan)
├── data-model.md        # Salida de la Fase 1 (/speckit-plan)
├── quickstart.md        # Salida de la Fase 1 (/speckit-plan)
├── contracts/           # Salida de la Fase 1 (/speckit-plan)
│   └── registrar-proveedor.yaml
└── tasks.md             # Salida de la Fase 2 (/speckit-tasks - NO se crea en /speckit-plan)

docs/adr/                # Decisiones arquitectónicas significativas (ver Investigación)
```

### Código Fuente (raíz del repositorio)

Se utiliza la estructura de solución solicitada, con cuatro proyectos de producción organizados
por responsabilidad (no como aplicación mecánica de "Clean Architecture", sino porque cada
proyecto aísla una preocupación concreta: reglas de dominio, casos de uso, detalles técnicos de
infraestructura y entrada HTTP) más dos proyectos de pruebas:

```text
SupplierOnboarding.sln

src/
├── SupplierOnboarding.Domain/
│   └── Proveedores/
│       ├── Proveedor.cs                    # Entidad raíz de agregado; guarda de invariantes en el constructor
│       ├── EstadoProveedor.cs              # Enum: únicamente "Pendiente" en este alcance
│       ├── IdentificadorFiscalNormalizado.cs  # Value object: normalización (FR-010)
│       ├── CatalogoPaisesIso3166.cs        # Validación de país (FR-003), sin dependencias externas
│       ├── ResultadoAlmacenamientoProveedor.cs # Enum: Agregado | ConflictoDuplicado (sin excepciones)
│       └── IProveedorRepository.cs         # Puerto específico de dominio (Repository Pattern)
│
├── SupplierOnboarding.Application/
│   ├── Identidad/
│   │   └── IUsuarioActual.cs               # Puerto: identificador del usuario autenticado (sin ASP.NET Core)
│   └── Proveedores/
│       └── RegistrarProveedor/
│           ├── RegistrarProveedorComando.cs     # No incluye RegistradoPor (se obtiene de IUsuarioActual)
│           ├── RegistrarProveedorCasoDeUso.cs   # Orquesta validación + unicidad (vía IProveedorRepository) + TimeProvider + IUsuarioActual
│           ├── RegistrarProveedorResultado.cs   # Casos: Exito | ErroresValidacion | Duplicado
│           └── RegistrarProveedorValidador.cs   # Validación de entrada (ver research.md, ADR-0007)
│
├── SupplierOnboarding.Infrastructure/
│   └── Persistencia/
│       ├── SupplierOnboardingDbContext.cs
│       ├── Configuraciones/ProveedorConfiguracion.cs   # Índice único (Pais, IdentificadorFiscalNormalizado)
│       ├── ProveedorRepository.cs           # Traduce violación de índice único a ConflictoDuplicado (sin excepción de dominio)
│       └── Migraciones/
│
└── SupplierOnboarding.Api/
    ├── Program.cs                            # Composición: DI, EF Core, OpenTelemetry, endpoints
    ├── Identidad/
    │   └── UsuarioActualHttp.cs              # Implementación temporal de IUsuarioActual vía ClaimsPrincipal/HttpContext
    └── Proveedores/
        ├── ProveedorContratos.cs             # Contratos HTTP de entrada/salida; sin RegistradoPor en la solicitud
        └── ProveedorEndpoints.cs             # Minimal API: único POST delgado, delega al caso de uso

tests/
├── SupplierOnboarding.UnitTests/
│   ├── Domain/
│   └── Application/RegistrarProveedor/       # Fakes de IProveedorRepository e IUsuarioActual, sin infraestructura real
│
└── SupplierOnboarding.IntegrationTests/
    ├── Persistencia/                         # EF Core + SQL Server real (Testcontainers)
    └── Api/                                  # WebApplicationFactory + HTTP end-to-end (sustituye IUsuarioActual por un doble de prueba)
```

**Dirección de dependencias**: `Api → Application → Domain`. `Infrastructure → Domain` únicamente
(implementa `IProveedorRepository`, definido en `Domain`); `Infrastructure` no referencia
`Application`, porque no necesita ningún contrato definido allí. `Api` referencia además
`Infrastructure` solo como raíz de composición (registro de DI/EF Core en `Program.cs`). `Domain` y
`Application` no referencian `Infrastructure` ni paquetes de EF Core, ASP.NET Core o SQL Server,
cumpliendo el Principio VII de la Constitution. De forma análoga, `Api` implementa el puerto
`IUsuarioActual` definido en `Application` (inversión de dependencias, mismo patrón que
`IProveedorRepository`); `Application` no referencia `Api`, ASP.NET Core, `ClaimsPrincipal` ni
`HttpContext`.

**Structure Decision**: Se adopta la estructura de 4 proyectos de producción + 2 de pruebas
solicitada. La justificación no es "Clean Architecture" como fin en sí mismo, sino la necesidad
concreta de: (a) mantener las reglas de la entidad `Proveedor` (estado inicial, normalización)
libres de tecnología; (b) aislar el caso de uso `RegistrarProveedor` como unidad de orquestación
testeable sin base de datos, incluyendo la verificación de la regla de unicidad cross-aggregate vía
`IProveedorRepository`; (c) confinar EF Core/SQL Server a Infrastructure; (d) mantener la capa HTTP
delgada; y (e) permitir pruebas unitarias rápidas separadas de pruebas de integración reales, tal
como exige la restricción de Testing de este plan y el Principio VI de la Constitution.

## Complexity Tracking

> No aplica: el Constitution Check no identificó violaciones que requieran justificación adicional.
> La separación en 4 proyectos de producción está justificada funcionalmente en la sección
> "Structure Decision" anterior y no se considera una violación del Principio V (Simplicidad antes
> que Distribución), dado que continúa tratándose de una única aplicación desplegable.
