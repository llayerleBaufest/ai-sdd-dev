---

description: "Lista de tareas para la implementación de la funcionalidad"
---

# Tareas: Registrar Proveedor

**Entrada**: Documentos de diseño de `specs/001-registrar-proveedor/` (`spec.md`, `plan.md`,
`data-model.md`, `research.md`, `contracts/registrar-proveedor.yaml`, `quickstart.md`,
`checklists/`), más `.specify/memory/constitution.md` y los ADRs vigentes en `docs/adr/`.

**Prerrequisitos**: `plan.md`, `spec.md`, `data-model.md`, `research.md`, `contracts/` (todos
disponibles y vigentes; no se modifican reglas de negocio ni decisiones arquitectónicas aquí).

**Pruebas**: Se incluyen explícitamente, por instrucción del usuario. Las pruebas unitarias y de
integración forman parte de las tareas de implementación de cada capa, no de una fase final única.

**Organización**: Las tres historias de usuario de `spec.md` (US1 registro exitoso, US2 rechazo por
datos inválidos, US3 rechazo por duplicado) comparten la misma entidad `Proveedor` y el mismo caso
de uso `RegistrarProveedorCasoDeUso`, por lo que **no** pueden implementarse como incrementos
verticales independientes. Por eso las tareas se organizan por capa arquitectónica (Dominio,
Aplicación, Infraestructura, API, Pruebas de integración), en el orden solicitado, y cada tarea
indica entre corchetes a qué historia de usuario sirve (`[US1]`, `[US2]`, `[US3]`) cuando el
comportamiento es específico de una de ellas. Las tareas verdaderamente transversales (esqueleto de
proyecto, contratos DTO, wiring de DI, migraciones, fixtures de prueba, observabilidad,
verificación final) no llevan etiqueta de historia.

## Formato: `[ID] [P?] [Story?] Descripción`

- **[P]**: Puede ejecutarse en paralelo (archivo distinto, sin dependencias pendientes).
- **[Story]**: Historia de usuario a la que sirve la tarea (US1/US2/US3), cuando aplica.
- Cada descripción incluye la ruta de archivo exacta afectada.

## Convenciones de Ruta

Proyecto único (backend HTTP), según la estructura definida en `plan.md`:

```text
SupplierOnboarding.sln
src/SupplierOnboarding.Domain/Proveedores/
src/SupplierOnboarding.Application/Proveedores/RegistrarProveedor/
src/SupplierOnboarding.Application/Identidad/
src/SupplierOnboarding.Infrastructure/Persistencia/
src/SupplierOnboarding.Api/Proveedores/
src/SupplierOnboarding.Api/Identidad/
tests/SupplierOnboarding.UnitTests/
tests/SupplierOnboarding.IntegrationTests/
```

---

## Fase 1: Setup

**Propósito**: Inicialización de la solución y los proyectos.

- [X] T001 Crear `SupplierOnboarding.sln` en la raíz del repositorio y los 4 proyectos de
  producción (`src/SupplierOnboarding.Domain`, `src/SupplierOnboarding.Application`,
  `src/SupplierOnboarding.Infrastructure`, `src/SupplierOnboarding.Api`) más los 2 proyectos de
  pruebas (`tests/SupplierOnboarding.UnitTests`, `tests/SupplierOnboarding.IntegrationTests`),
  todos dirigidos a `net10.0` con C# 14, y agregarlos a la solución (ADR-0002).
- [X] T002 Configurar las referencias de proyecto según la dirección de dependencias del plan:
  `Application` → `Domain`; `Infrastructure` → `Domain` (sin referenciar `Application`); `Api` →
  `Application` e `Infrastructure`; `UnitTests` → `Domain` y `Application`; `IntegrationTests` →
  `Infrastructure` y `Api`. Depende de T001.
- [X] T003 [P] Agregar a `src/SupplierOnboarding.Infrastructure/SupplierOnboarding.Infrastructure.csproj`
  los paquetes `Microsoft.EntityFrameworkCore.SqlServer` y `Microsoft.EntityFrameworkCore.Design`
  (EF Core 10, ADR-0003).
- [X] T004 [P] Agregar a `src/SupplierOnboarding.Api/SupplierOnboarding.Api.csproj` los paquetes
  `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`,
  `OpenTelemetry.Instrumentation.EntityFrameworkCore` y un exportador OTLP/consola (ADR-0008).
- [X] T005 [P] Agregar a `tests/SupplierOnboarding.UnitTests/SupplierOnboarding.UnitTests.csproj`
  los paquetes `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` y
  `Microsoft.Extensions.TimeProvider.Testing` (para `FakeTimeProvider` determinístico).
- [X] T006 [P] Agregar a
  `tests/SupplierOnboarding.IntegrationTests/SupplierOnboarding.IntegrationTests.csproj` los
  paquetes `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Testcontainers.MsSql` y
  `Microsoft.AspNetCore.Mvc.Testing` (ADR-0006).
- [X] T007 [P] Configurar propiedades comunes (`LangVersion` C# 14, `Nullable=enable`,
  `ImplicitUsings=enable`, `TargetFramework=net10.0`) en un `Directory.Build.props` en la raíz del
  repositorio, aplicado a los 6 proyectos.

**Checkpoint**: Solución compilable con proyectos vacíos y referencias correctas.

---

## Fase 2: Fundacional (bloqueante)

**Propósito**: Infraestructura mínima compartida que deben usar todas las historias.

⚠️ Ninguna tarea de Dominio/Aplicación/Infraestructura/API puede darse por completa sin esto.

- [X] T008 [P] Crear `src/SupplierOnboarding.Api/appsettings.json` y
  `appsettings.Development.json` con una sección `ConnectionStrings:SupplierOnboarding` vacía
  (sin credenciales reales en el repositorio, Principio II de la Constitution).
- [X] T009 Crear el esqueleto de `src/SupplierOnboarding.Api/Program.cs` con
  `WebApplication.CreateBuilder`/`builder.Build()`, sin endpoints de negocio todavía (se completan
  en la Fase API). Depende de T001, T002.
- [X] T010 En `src/SupplierOnboarding.Api/Program.cs`, configurar manejo uniforme de errores no
  controlados mediante `ProblemDetails` (`AddProblemDetails()` + `UseExceptionHandler()`),
  aplicable a cualquier endpoint futuro. Depende de T009.
- [X] T011 En `src/SupplierOnboarding.Api/Program.cs`, registrar `TimeProvider.System` en el
  contenedor de DI (`builder.Services.AddSingleton(TimeProvider.System)`) como única fuente del
  instante de registro que `Application` pasará a `Domain` (research.md punto 10; ningún reloj
  global se invoca en `Domain`). Depende de T009.

**Checkpoint**: Esqueleto de API compilable, sin lógica de negocio; listo para Dominio/Aplicación.

---

## Fase 3: Dominio

**Propósito**: Entidad `Proveedor` y sus colaboradores, protegiendo sus invariantes de forma
independiente de la validación de entrada de `Application` (data-model.md).

### Pruebas de Dominio (escribir primero; deben fallar antes de implementar)

- [X] T012 [US1] Prueba unitaria de creación válida de `Proveedor` con datos válidos en
  `tests/SupplierOnboarding.UnitTests/Domain/ProveedorTests.cs`, verificando que expone los campos
  ingresados y que `Estado` inicia en `Pendiente` (FR-012).
- [X] T013 [US1] En `ProveedorTests.cs`, prueba unitaria que verifique que ninguna operación del
  dominio permite construir un `Proveedor` con un estado inicial distinto de `Pendiente` (FR-013).
  Depende de T012 (mismo archivo).
- [X] T014 [US2] En `ProveedorTests.cs`, pruebas unitarias que verifiquen que el constructor de
  `Proveedor` rechaza (guard clauses), de forma independiente entre sí, razón social vacía/solo
  espacios, país inválido, identificador fiscal vacío, nombre de contacto vacío y correo con
  formato inválido — invariantes del dominio (FR-002 a FR-006), independientes de
  `RegistrarProveedorValidador`. Depende de T013 (mismo archivo).
- [X] T015 [US1] En `ProveedorTests.cs`, prueba unitaria que verifique que `Id` se genera de forma
  determinística con `Guid.CreateVersion7(instante)` a partir de un instante fijo recibido como
  parámetro (ADR-0004), sin invocar reloj global — comportamiento determinístico de fecha/hora.
  Depende de T014 (mismo archivo).
- [X] T016 [P] [US3] Prueba unitaria de normalización (FR-010) en
  `tests/SupplierOnboarding.UnitTests/Domain/IdentificadorFiscalNormalizadoTests.cs`: elimina
  espacios, convierte a mayúsculas y elimina `-`, `.`, `/`, con casos representativos de formatos
  irrelevantes equivalentes.
- [X] T017 [US3] En `IdentificadorFiscalNormalizadoTests.cs`, prueba que documente el caso límite
  abierto CHK029 (identificador compuesto solo por separadores/espacios produce texto normalizado
  vacío): confirmar que no se agrega ninguna regla de rechazo nueva no solicitada y dejar
  constancia en el comentario de la prueba para revisión de negocio (data-model.md). Depende de
  T016 (mismo archivo).
- [X] T018 [P] [US2] Prueba unitaria en
  `tests/SupplierOnboarding.UnitTests/Domain/CatalogoPaisesIso3166Tests.cs` que verifique que
  países soberanos ISO 3166-1 son aceptados y que territorios dependientes/códigos inválidos son
  rechazados (FR-003).

### Implementación de Dominio

- [X] T019 [P] [US1] Implementar `EstadoProveedor` (enum con único valor `Pendiente`) en
  `src/SupplierOnboarding.Domain/Proveedores/EstadoProveedor.cs` (FR-012, FR-013).
- [X] T020 [P] [US2] Implementar `CatalogoPaisesIso3166` (catálogo estático de países soberanos
  ISO 3166-1, sin dependencias externas) en
  `src/SupplierOnboarding.Domain/Proveedores/CatalogoPaisesIso3166.cs` (FR-003). Hace pasar T018.
- [X] T021 [P] [US3] Implementar el value object `IdentificadorFiscalNormalizado` en
  `src/SupplierOnboarding.Domain/Proveedores/IdentificadorFiscalNormalizado.cs` con la
  transformación de FR-010. Hace pasar T016 y T017.
- [X] T022 [US1] Implementar la entidad `Proveedor` (raíz de agregado) en
  `src/SupplierOnboarding.Domain/Proveedores/Proveedor.cs`: constructor con guard clauses
  independientes (razón social, país vía `CatalogoPaisesIso3166`, identificador fiscal, nombre de
  contacto, correo), cálculo de `IdentificadorFiscalNormalizado`, generación de `Id` con
  `Guid.CreateVersion7(instante)` recibiendo el instante como parámetro, `Estado = Pendiente` fijo,
  y almacenamiento de `RegistradoPor`/`RegistradoEn`. Depende de T019, T020, T021. Hace pasar T012,
  T013, T014, T015.
- [X] T023 [P] [US3] Implementar `ResultadoAlmacenamientoProveedor` (enum `Agregado` |
  `ConflictoDuplicado`) en
  `src/SupplierOnboarding.Domain/Proveedores/ResultadoAlmacenamientoProveedor.cs`, como resultado
  funcional de persistencia sin excepción de dominio (ADR-0005; no se crea
  `ProveedorDuplicadoException`).
- [X] T024 Definir el puerto `IProveedorRepository` en
  `src/SupplierOnboarding.Domain/Proveedores/IProveedorRepository.cs` con únicamente
  `Task<bool> ExisteAsync(string pais, string identificadorFiscalNormalizado, CancellationToken ct)`
  y `Task<ResultadoAlmacenamientoProveedor> AgregarAsync(Proveedor proveedor, CancellationToken ct)`
  — Repository Pattern específico, sin `GenericRepository<T>` (Principio VII). Depende de T022,
  T023.

**Checkpoint**: `Proveedor` protege sus invariantes de forma aislada; el puerto del repositorio
está definido; ninguna regla de unicidad cross-aggregate vive todavía aquí (se resuelve en
Aplicación e Infraestructura).

---

## Fase 4: Aplicación

**Propósito**: Caso de uso `RegistrarProveedor`, que orquesta validación de entrada, la regla de
negocio cross-aggregate de unicidad (vía `IProveedorRepository`) y la construcción del `Proveedor`
con el instante obtenido de `TimeProvider` y el identificador del usuario autenticado obtenido de
`IUsuarioActual` (corrección U1: `RegistradoPor` nunca proviene del cliente HTTP, sino de la
identidad autenticada).

### Pruebas de Aplicación (escribir primero; deben fallar antes de implementar)

- [ ] T025 [US2] Prueba unitaria en
  `tests/SupplierOnboarding.UnitTests/Application/RegistrarProveedor/RegistrarProveedorValidadorTests.cs`
  que verifique que `RegistrarProveedorValidador` acumula **todos** los errores cuando varios
  datos obligatorios son inválidos simultáneamente (Caso Límite de `spec.md`, FR-017), en lugar de
  detenerse en el primero.
- [ ] T026 [US2] En `RegistrarProveedorValidadorTests.cs`, pruebas unitarias individuales para cada
  regla FR-002 a FR-006. Depende de T025 (mismo archivo).
- [ ] T027 [P] Crear el fake en memoria `ProveedorRepositoryFake` (implementación de
  `IProveedorRepository` para pruebas, sin biblioteca de mocking) en
  `tests/SupplierOnboarding.UnitTests/Application/RegistrarProveedor/ProveedorRepositoryFake.cs`
  (research.md punto 9). Depende de T024.
- [ ] T028 [P] Crear el fake `UsuarioActualFake` (implementación de `IUsuarioActual` para pruebas
  que permite fijar explícitamente el identificador del usuario actual, sin biblioteca de
  mocking) en
  `tests/SupplierOnboarding.UnitTests/Application/RegistrarProveedor/UsuarioActualFake.cs`
  (corrección U1: la identidad del usuario debe poder controlarse de forma determinística en
  pruebas unitarias).
- [ ] T029 [US1] Prueba unitaria en
  `tests/SupplierOnboarding.UnitTests/Application/RegistrarProveedor/RegistrarProveedorCasoDeUsoTests.cs`
  para el registro válido: usando `ProveedorRepositoryFake`, un `FakeTimeProvider` con instante
  fijo y `UsuarioActualFake` con un identificador de usuario fijo, verificar que el resultado es
  `Exito`, el proveedor queda `Pendiente`, y que `RegistradoEn`, `Id` y `RegistradoPor` son
  determinísticos según ese instante y ese identificador fijos (FR-015). Depende de T027, T028.
- [ ] T030 [US3] En `RegistrarProveedorCasoDeUsoTests.cs`, prueba que verifique que si
  `IProveedorRepository.ExisteAsync` (con el identificador fiscal normalizado) devuelve `true`, el
  caso de uso devuelve `Duplicado` sin invocar `AgregarAsync` (FR-009). Depende de T029 (mismo
  archivo).
- [ ] T031 [US3] En `RegistrarProveedorCasoDeUsoTests.cs`, prueba que verifique que si `ExisteAsync`
  devuelve `false` pero `AgregarAsync` devuelve `ResultadoAlmacenamientoProveedor.ConflictoDuplicado`
  (condición de carrera), el caso de uso también devuelve `Duplicado`, sin lanzar excepción
  (ADR-0005). Depende de T030 (mismo archivo).
- [ ] T032 [US2] En `RegistrarProveedorCasoDeUsoTests.cs`, prueba que verifique que datos de entrada
  inválidos detienen el flujo antes de consultar `IProveedorRepository` y devuelven
  `ErroresValidacion` con todos los errores acumulados. Depende de T031 (mismo archivo).

### Implementación de Aplicación

- [ ] T033 [P] Implementar `RegistrarProveedorComando` (datos de entrada del caso de uso: razón
  social, país, identificador fiscal, nombre de contacto, correo de contacto) en
  `src/SupplierOnboarding.Application/Proveedores/RegistrarProveedor/RegistrarProveedorComando.cs`
  (FR-001). `RegistrarProveedorComando` NO incluye `RegistradoPor`: el caso de uso lo obtiene de
  `IUsuarioActual`, nunca del cliente HTTP (corrección U1).
- [ ] T034 [P] Implementar `RegistrarProveedorResultado` (casos `Exito` | `ErroresValidacion` |
  `Duplicado`) en
  `src/SupplierOnboarding.Application/Proveedores/RegistrarProveedor/RegistrarProveedorResultado.cs`.
- [ ] T035 [P] Definir el puerto `IUsuarioActual` en
  `src/SupplierOnboarding.Application/Identidad/IUsuarioActual.cs`: expone únicamente el
  identificador del usuario autenticado (por ejemplo, `string IdentificadorUsuario { get; }`), sin
  ninguna dependencia de ASP.NET Core, `ClaimsPrincipal` ni `HttpContext` en la propia interfaz
  (Principio VI de la Constitution). La autenticación/autorización concretas continúan fuera de
  alcance (FR-019); esta interfaz solo prepara el punto de extensión (corrección U1). Hace pasar
  T028.
- [ ] T036 [US2] Implementar `RegistrarProveedorValidador` en
  `src/SupplierOnboarding.Application/Proveedores/RegistrarProveedor/RegistrarProveedorValidador.cs`:
  validación manual y explícita (sin FluentValidation, ADR-0007) que evalúa de forma independiente
  y acumula errores para FR-002 a FR-006 — validación de entrada, distinta de los invariantes de
  `Proveedor`. Depende de T033. Hace pasar T025 y T026.
- [ ] T037 Implementar `RegistrarProveedorCasoDeUso` en
  `src/SupplierOnboarding.Application/Proveedores/RegistrarProveedor/RegistrarProveedorCasoDeUso.cs`:
  ejecuta `RegistrarProveedorValidador`; si hay errores retorna `ErroresValidacion`; si no, obtiene
  el instante vía `TimeProvider` inyectado y el identificador del usuario actual vía
  `IUsuarioActual.IdentificadorUsuario` inyectado (corrección U1: `RegistradoPor` se obtiene
  exclusivamente de esta dependencia, nunca del comando ni del cliente HTTP), y consulta
  `IProveedorRepository.ExisteAsync(pais, identificadorFiscalNormalizado)` — regla de negocio
  cross-aggregate (FR-007/FR-008/FR-009), distinta de los invariantes de dominio; si existe retorna
  `Duplicado`; si no, construye `Proveedor` (delegando sus invariantes al dominio, pasándole el
  instante y el `RegistradoPor` obtenidos) y llama `AgregarAsync`, interpretando
  `ConflictoDuplicado` como `Duplicado` y `Agregado` como `Exito`. Depende de T022, T024, T034,
  T035, T036. Hace pasar T029, T030, T031, T032.

**Checkpoint**: El caso de uso cubre las tres historias de usuario de forma aislada, sin
infraestructura real (unit tests con fakes en memoria de `IProveedorRepository` e
`IUsuarioActual`).

---

## Fase 5: Infraestructura

**Propósito**: Persistencia con EF Core 10 + SQL Server, incluyendo la restricción `UNIQUE` que
protege la regla de unicidad ante condiciones de carrera (data-model.md, ADR-0005).

- [ ] T038 Implementar `SupplierOnboardingDbContext` (con `DbSet<Proveedor> Proveedores`) en
  `src/SupplierOnboarding.Infrastructure/Persistencia/SupplierOnboardingDbContext.cs`. Depende de
  T022.
- [ ] T039 Implementar `ProveedorConfiguracion` (`IEntityTypeConfiguration<Proveedor>`) en
  `src/SupplierOnboarding.Infrastructure/Persistencia/Configuraciones/ProveedorConfiguracion.cs`:
  mapear `Proveedor` con columnas separadas para `IdentificadorFiscal` (valor original) e
  `IdentificadorFiscalNormalizado` (valor derivado); definir el índice único sobre
  `(Pais, IdentificadorFiscalNormalizado)` — restricción de persistencia que refuerza FR-007/FR-008
  además de la verificación previa en Aplicación (ADR-0005); no se define restricción de unicidad
  sobre `RazonSocial` (FR-020). Depende de T038.
- [ ] T040 Registrar `ProveedorConfiguracion` en `OnModelCreating` de
  `src/SupplierOnboarding.Infrastructure/Persistencia/SupplierOnboardingDbContext.cs`. Depende de
  T039.
- [ ] T041 Implementar `ProveedorRepository` en
  `src/SupplierOnboarding.Infrastructure/Persistencia/ProveedorRepository.cs`: `ExisteAsync`
  consulta por `(Pais, IdentificadorFiscalNormalizado)`; `AgregarAsync` intenta `Add` +
  `SaveChangesAsync`, capturando la violación del índice único (`DbUpdateException`) y
  traduciéndola a `ResultadoAlmacenamientoProveedor.ConflictoDuplicado`, sin propagar excepciones de
  EF Core/SQL Server fuera de `Infrastructure` ni excepciones de dominio. Depende de T040, T024.
- [ ] T042 [P] Generar la migración inicial de EF Core
  (`dotnet ef migrations add InicialProveedor --project src/SupplierOnboarding.Infrastructure
  --startup-project src/SupplierOnboarding.Api`) en
  `src/SupplierOnboarding.Infrastructure/Persistencia/Migraciones/`, verificando que incluya el
  índice único sobre `(Pais, IdentificadorFiscalNormalizado)`. Depende de T040.

**Checkpoint**: Persistencia real disponible; la unicidad está protegida en dos niveles
(Aplicación + índice `UNIQUE`), sin excepciones de dominio para el caso de duplicado.

---

## Fase 6: API

**Propósito**: Único endpoint `POST /api/proveedores` (Minimal API), delgado, que traduce el
resultado del caso de uso a las respuestas HTTP del contrato. No se agrega
`GET /api/proveedores/{id}` (eliminado del plan por ausencia de requisito que lo justifique).
`RegistradoPor` nunca se acepta desde el cliente HTTP (corrección U1): se resuelve dentro del caso
de uso a partir de `IUsuarioActual`.

- [ ] T043 [P] Implementar los contratos HTTP (`RegistrarProveedorSolicitud`, `ProveedorRespuesta`,
  `ErroresValidacion`, `ErrorDuplicado`) en
  `src/SupplierOnboarding.Api/Proveedores/ProveedorContratos.cs`, alineados exactamente con
  `contracts/registrar-proveedor.yaml`. Este archivo contiene exclusivamente contratos de entrada y
  salida HTTP para "Registrar proveedor", sin reglas de negocio, lógica de dominio, persistencia
  ni dependencias de Entity Framework Core; confirmar que `RegistrarProveedorSolicitud` NO incluye
  `RegistradoPor` (corrección I1/U1: se obtiene de la identidad autenticada, nunca del cliente).
- [ ] T044 Implementar `UsuarioActualHttp` (implementación temporal de `IUsuarioActual`) en
  `src/SupplierOnboarding.Api/Identidad/UsuarioActualHttp.cs`, que obtiene el identificador del
  usuario desde el `ClaimsPrincipal`/`HttpContext` actual (por ejemplo vía
  `IHttpContextAccessor`); marcador explícito y temporal hasta integrar un proveedor de
  autenticación real (por ejemplo Microsoft Entra ID), que continúa fuera de alcance de esta
  feature (FR-019). Si no hay identidad autenticada disponible en `HttpContext.User` (caso
  esperado mientras no exista autenticación real configurada), debe devolver un valor de
  marcador temporal y documentado (por ejemplo `"sistema"` o `"desconocido"`) en lugar de un
  valor vacío o nulo, para que `RegistradoPor` nunca quede sin valor (FR-015) al ejecutar la app
  real (por ejemplo, durante T062); esto no introduce una regla de negocio nueva, solo evita un
  valor indefinido en un campo obligatorio. No implementa Microsoft Entra ID ni ningún otro
  proveedor concreto de autenticación todavía. Depende de T035.
- [ ] T045 [US1] Implementar `ProveedorEndpoints.cs` en
  `src/SupplierOnboarding.Api/Proveedores/ProveedorEndpoints.cs`: método de extensión que mapea
  `POST /api/proveedores`, traduce `RegistrarProveedorSolicitud` a `RegistrarProveedorComando`,
  invoca `RegistrarProveedorCasoDeUso` y traduce el caso `Exito` del resultado a `201 Created` con
  `ProveedorRespuesta` (FR-016). Depende de T037, T043.
- [ ] T046 [US2] En `ProveedorEndpoints.cs`, mapear el caso `ErroresValidacion` del resultado a
  `400 Bad Request` con el cuerpo `ErroresValidacion` listando todos los campos inválidos (FR-017).
  Depende de T045 (mismo archivo).
- [ ] T047 [US3] En `ProveedorEndpoints.cs`, mapear el caso `Duplicado` del resultado a
  `409 Conflict` con el cuerpo `ErrorDuplicado`, sin exponer identidad interna, razón social ni
  estado del proveedor existente (FR-018). Depende de T046 (mismo archivo).
- [ ] T048 Completar `src/SupplierOnboarding.Api/Program.cs`: registrar
  `SupplierOnboardingDbContext` con SQL Server usando la cadena de conexión de configuración,
  registrar `IProveedorRepository` → `ProveedorRepository`, registrar `IUsuarioActual` →
  `UsuarioActualHttp` (corrección U1), registrar `RegistrarProveedorCasoDeUso`, y mapear
  `ProveedorEndpoints`. Depende de T009, T011, T041, T044, T047.

**Checkpoint**: Las tres historias de usuario son alcanzables de punta a punta vía HTTP;
`RegistradoPor` se resuelve siempre desde la identidad autenticada, nunca desde el body de la
solicitud.

---

## Fase 7: Pruebas de Integración

**Propósito**: Validar `Infrastructure` y el endpoint HTTP contra un SQL Server real
(Testcontainers), incluyendo la protección real ante condiciones de carrera (ADR-0006).

- [ ] T049 [P] Crear la fixture de Testcontainers (`SqlServerContainerFixture`,
  `ICollectionFixture`) en
  `tests/SupplierOnboarding.IntegrationTests/Persistencia/SqlServerContainerFixture.cs`, que
  levanta un contenedor SQL Server real y aplica las migraciones (`Database.Migrate()`) al
  iniciar. Depende de T042.
- [ ] T050 [US1] Prueba de integración en
  `tests/SupplierOnboarding.IntegrationTests/Persistencia/ProveedorRepositoryTests.cs` que
  verifique que `ProveedorRepository.AgregarAsync` persiste un `Proveedor` válido contra SQL Server
  real y que `ExisteAsync` lo encuentra por `(Pais, IdentificadorFiscalNormalizado)`. Depende de
  T049, T041.
- [ ] T051 [US3] En `ProveedorRepositoryTests.cs`, prueba que verifique que el índice único
  `(Pais, IdentificadorFiscalNormalizado)` rechaza una segunda inserción con la misma combinación
  (incluida una variante de formato irrelevante), devolviendo `ConflictoDuplicado`. Depende de T050
  (mismo archivo).
- [ ] T052 [US3] En `ProveedorRepositoryTests.cs`, prueba que dispare dos inserciones concurrentes
  (`Task.WhenAll`) con la misma combinación de país e identificador fiscal directamente contra
  `ProveedorRepository`/`SupplierOnboardingDbContext`, verificando que solo una tenga éxito
  (`Agregado`) y la otra `ConflictoDuplicado` — protección real ante condiciones de carrera (Caso
  Límite de `spec.md`). Depende de T051 (mismo archivo).
- [ ] T053 [P] [US1] Prueba de integración end-to-end en
  `tests/SupplierOnboarding.IntegrationTests/Api/ProveedorEndpointsTests.cs` (usando
  `WebApplicationFactory` contra la base de datos del contenedor, sustituyendo `IUsuarioActual` por
  un doble de prueba con un identificador de usuario fijo mediante `ConfigureTestServices` — no
  hay autenticación real configurada en esta feature, corrección U1) que verifique que
  `POST /api/proveedores` con datos válidos devuelve `201 Created` con `ProveedorRespuesta`
  conteniendo `estado: Pendiente`, y `registradoPor`/`registradoEn` coincidiendo con el
  identificador y el instante del doble de prueba (FR-015, FR-016). Depende de T049, T048.
- [ ] T054 [US2] En `ProveedorEndpointsTests.cs`, prueba que verifique que
  `POST /api/proveedores` con los cinco datos obligatorios inválidos simultáneamente devuelve
  `400 Bad Request` con los cinco errores listados (FR-017). Depende de T053 (mismo archivo).
- [ ] T055 [US3] En `ProveedorEndpointsTests.cs`, prueba que registre un proveedor válido y luego
  repita el registro con la misma combinación exacta de país e identificador fiscal, verificando
  `409 Conflict` con `ErrorDuplicado` sin datos del proveedor existente (FR-009, FR-018). Depende de
  T054 (mismo archivo).
- [ ] T056 [US3] En `ProveedorEndpointsTests.cs`, prueba que repita el escenario anterior con una
  variante de formato irrelevante del identificador fiscal (espacios, mayúsculas/minúsculas o
  separadores distintos), verificando igualmente `409 Conflict` (FR-010). Depende de T055 (mismo
  archivo).
- [ ] T057 [US1] En `ProveedorEndpointsTests.cs`, prueba que registre dos proveedores en países
  distintos con el mismo valor textual de identificador fiscal, verificando que ambos devuelven
  `201 Created` como entidades independientes (FR-011, SC-006). Depende de T056 (mismo archivo).
- [ ] T058 En `ProveedorEndpointsTests.cs`, prueba que registre dos proveedores con la misma razón
  social pero distinta combinación de país e identificador fiscal, verificando que ambos devuelven
  `201 Created` (FR-020; sin historia de usuario explícita asociada, ver CHK019). Depende de T057
  (mismo archivo).

**Checkpoint**: Las tres historias de usuario están verificadas de punta a punta contra
infraestructura real.

---

## Fase 8: Observabilidad

**Propósito**: Trazas, métricas y logging estructurado desde el diseño (Principio III, ADR-0008).

- [ ] T059 Configurar OpenTelemetry en `src/SupplierOnboarding.Api/Program.cs`: trazas y métricas
  con instrumentación de ASP.NET Core, EF Core y `SqlClient`, exportando a consola/OTLP local.
  Depende de T048.
- [ ] T060 [P] Configurar logging estructurado vía `ILogger` correlacionado por `Activity` en
  `src/SupplierOnboarding.Application/Proveedores/RegistrarProveedor/RegistrarProveedorCasoDeUso.cs`
  y `src/SupplierOnboarding.Api/Proveedores/ProveedorEndpoints.cs` (registrar intentos de registro,
  duplicados y errores de validación, sin datos sensibles). Depende de T037, T045.

**Checkpoint**: Observabilidad mínima operativa sin requerir Azure Application Insights.

---

## Fase 9: Verificación Final

**Propósito**: Confirmar que la implementación cumple `spec.md`, `plan.md` y los checklists
vigentes, sin crear todavía recursos Azure.

- [ ] T061 Ejecutar `dotnet test tests/SupplierOnboarding.UnitTests` y
  `dotnet test tests/SupplierOnboarding.IntegrationTests`, verificando que toda la suite pasa.
  Depende de T012–T060.
- [ ] T062 Ejecutar manualmente los tres escenarios de
  `specs/001-registrar-proveedor/quickstart.md` (registro exitoso, datos inválidos, duplicado)
  contra la API en ejecución y confirmar que las respuestas coinciden con lo documentado. Depende
  de T061.
- [ ] T063 Revisar `specs/001-registrar-proveedor/quickstart.md` y actualizarlo solo si algún paso
  de puesta en marcha cambió durante la implementación (por ejemplo, el nombre exacto de la
  migración `InicialProveedor`). Depende de T062.
- [ ] T064 [P] Verificar la tabla de trazabilidad de este documento contra `spec.md`: confirmar que
  cada requisito funcional (FR-001 a FR-020) y cada criterio de éxito (SC-001 a SC-006) tiene al
  menos una tarea de implementación o prueba asociada. Depende de T061.
- [ ] T065 [P] Revisar `specs/001-registrar-proveedor/checklists/calidad.md` (ítems CHK008, CHK028,
  CHK029 sobre el identificador fiscal) y dejar constancia, en el propio código (comentarios de
  T017) o en la revisión de esta tarea, de que no se resolvieron inventando nuevas reglas de
  negocio no aprobadas. Depende de T061.

---

## Dependencias y Orden de Ejecución

### Dependencias entre Fases

- **Setup (Fase 1)**: sin dependencias.
- **Fundacional (Fase 2)**: depende de Setup.
- **Dominio (Fase 3)**: depende de Fundacional (proyectos referenciados y compilables).
- **Aplicación (Fase 4)**: depende de Dominio (T022, T023, T024).
- **Infraestructura (Fase 5)**: depende de Dominio (T022, T024); no depende de Aplicación
  (`Infrastructure → Domain` únicamente).
- **API (Fase 6)**: depende de Aplicación (T037) e Infraestructura (T041).
- **Pruebas de integración (Fase 7)**: depende de Infraestructura (T042) y API (T048).
- **Observabilidad (Fase 8)**: depende de API (T048).
- **Verificación final (Fase 9)**: depende de todas las fases anteriores.

### Dentro de cada fase

- Las pruebas se escriben antes o junto con la implementación que verifican (indicado en cada
  tarea con "Hace pasar TXXX").
- Los archivos compartidos (por ejemplo, `ProveedorTests.cs`, `ProveedorEndpoints.cs`,
  `ProveedorEndpointsTests.cs`) se editan de forma secuencial, no en paralelo.

### Oportunidades de Paralelismo

- Fase 1: T003–T007 en paralelo entre sí (tras T001/T002).
- Fase 3: T016 y T018 en paralelo entre sí y respecto a T012–T015 (archivos distintos); T019, T020,
  T021, T023 en paralelo entre sí (archivos distintos, sin dependencias cruzadas).
- Fase 4: T027 y T028 en paralelo entre sí y con T025/T026 (archivos distintos); T033, T034 y T035
  en paralelo entre sí (archivos distintos).
- Fase 5: T042 en paralelo con T041 (ambos dependen solo de T040).
- Fase 6: T043 en paralelo con el resto de la fase (no depende de T037); T044 puede iniciarse en
  paralelo con T043 (archivos distintos).
- Fase 7: T049 puede iniciarse en paralelo con el resto de Infraestructura/API que no dependan de
  ella; T053 puede iniciarse en paralelo con T050–T052 (archivos distintos).
- Fase 8: T060 en paralelo con T059 (archivos distintos).
- Fase 9: T064 y T065 en paralelo entre sí (ambos solo dependen de T061).

---

## Ejemplo de Ejecución en Paralelo (Fase 3, Dominio)

```bash
# Pruebas independientes por archivo:
Task: "Prueba de normalización en tests/.../Domain/IdentificadorFiscalNormalizadoTests.cs (T016)"
Task: "Prueba de catálogo de países en tests/.../Domain/CatalogoPaisesIso3166Tests.cs (T018)"

# Implementación de colaboradores independientes de Proveedor:
Task: "Implementar EstadoProveedor.cs (T019)"
Task: "Implementar CatalogoPaisesIso3166.cs (T020)"
Task: "Implementar IdentificadorFiscalNormalizado.cs (T021)"
Task: "Implementar ResultadoAlmacenamientoProveedor.cs (T023)"
```

---

## Estrategia de Implementación

1. Completar Fase 1 (Setup) y Fase 2 (Fundacional) — bloqueante para todo lo demás.
2. Completar Fase 3 (Dominio): entidad `Proveedor` con invariantes propias, verificable con
   pruebas unitarias sin ninguna otra capa.
3. Completar Fase 4 (Aplicación): caso de uso completo, verificable con pruebas unitarias y fakes,
   sin infraestructura real. **Punto de validación**: las tres historias de usuario ya son
   observables a nivel de caso de uso (`Exito` / `ErroresValidacion` / `Duplicado`).
4. Completar Fase 5 (Infraestructura) y Fase 6 (API): habilita el flujo HTTP real de punta a punta.
5. Completar Fase 7 (Pruebas de integración): confirma el comportamiento contra SQL Server real,
   incluida la protección ante condiciones de carrera.
6. Completar Fase 8 (Observabilidad) y Fase 9 (Verificación final).

No se crean recursos Azure en ninguna fase de esta lista de tareas.

---

## Tabla de Trazabilidad (spec.md → tareas)

| Requisito / Criterio | Tareas |
|---|---|
| FR-001 (datos mínimos obligatorios) | T033, T043, T045 |
| FR-002 (razón social vacía) | T014, T025, T026, T036 |
| FR-003 (país inválido/no soportado) | T014, T018, T020, T036 |
| FR-004 (identificador fiscal vacío) | T014, T036 |
| FR-005 (nombre de contacto vacío) | T014, T036 |
| FR-006 (correo inválido) | T014, T036 |
| FR-007 (unicidad país + identificador) | T023, T024, T039, T041, T050, T051 |
| FR-008 (sin excepción, cualquier estado) | T039, T041, T051, T052, T055 |
| FR-009 (rechazar e informar duplicado) | T030, T037, T047, T055 |
| FR-010 (normalización del identificador fiscal) | T016, T017, T021, T056 |
| FR-011 (mismo identificador, país distinto) | T057 |
| FR-012 (estado Pendiente automático) | T012, T019, T022 |
| FR-013 (ningún otro estado inicial) | T013, T019, T022 |
| FR-014 (identidad propia única, GUID v7) | T015, T022, T024 |
| FR-015 (auditoría: quién y cuándo) | T022, T029, T035, T037, T053 |
| FR-016 (confirmación de éxito) | T045, T053 |
| FR-017 (informar todos los datos a corregir) | T025, T026, T036, T046, T054 |
| FR-018 (mensaje de duplicado sin exponer datos) | T047, T055 |
| FR-019 (autenticación/autorización) | Fuera de alcance (no se implementa un proveedor de autenticación real); la arquitectura queda preparada mediante el puerto `IUsuarioActual` (T035) y su implementación temporal `UsuarioActualHttp` (T044), sin exponer `RegistradoPor` como dato editable por el cliente (corrección U1) |
| FR-020 (razón social no única) | T039, T058 |
| SC-001 (registro exitoso en un intento) | T053, T062 |
| SC-002 (100% rechazos por datos inválidos) | T054, T062 |
| SC-003 (100% rechazos por duplicado) | T055, T056, T062 |
| SC-004 (100% quedan en Pendiente) | T012, T053 |
| SC-005 (auditoría verificable) | T029, T053 |
| SC-006 (países distintos, mismo identificador) | T057 |

---

## Notas

- `[P]` = archivos distintos, sin dependencias pendientes entre sí.
- `[Story]` traza la tarea a la historia de usuario que verifica, cuando aplica; las tareas
  transversales (contratos DTO, DI, migraciones, fixtures, observabilidad, verificación final) no
  llevan etiqueta.
- No se incorpora `GET /api/proveedores/{id}` (sin requisito que lo justifique).
- No se utiliza `GenericRepository<T>`; `IProveedorRepository` expone únicamente `ExisteAsync` y
  `AgregarAsync`.
- No se introduce `ProveedorDuplicadoException` en `Domain`; el duplicado se modela como
  `ResultadoAlmacenamientoProveedor.ConflictoDuplicado` (resultado funcional) y como
  `RegistrarProveedorResultado.Duplicado` en `Application`.
- `Domain` nunca invoca `DateTime.Now` ni `DateTimeOffset.UtcNow`; el instante de registro se
  obtiene en `Application` vía `TimeProvider` y se pasa como parámetro.
- `RegistradoPor` nunca proviene del cliente HTTP: el caso de uso lo obtiene del puerto
  `IUsuarioActual` (definido en `Application`, implementado temporalmente en `Api` sin un proveedor
  de autenticación real todavía); `RegistrarProveedorSolicitud`/`RegistrarProveedorComando` no
  incluyen este campo (corrección U1).
- No se crean recursos Azure en esta lista de tareas.
