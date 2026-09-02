# Modelo de Datos (Fase 1): Registrar Proveedor

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Investigación**:
[research.md](./research.md)

Este documento describe el modelo de dominio derivado de la sección "Entidades Clave" y de los
Requisitos Funcionales de `spec.md`. No se agregan atributos, entidades ni comportamientos fuera de
lo exigido por la especificación vigente.

## Entidad: Proveedor

Representa a una organización externa que inicia un proceso de onboarding (ver spec.md, sección
"Entidades Clave"). Es la raíz de agregado del dominio en esta funcionalidad.

| Campo | Tipo | Obligatorio | Origen / Regla | Notas técnicas |
|---|---|---|---|---|
| `Id` | GUID (v7) | Sí | FR-014: identidad propia y única, formato no impuesto por la especificación | Generado por el dominio con `Guid.CreateVersion7(instante)` a partir del instante de registro recibido como parámetro (ver [ADR-0004](../../docs/adr/ADR-0004-formato-identificador-interno.md)); clave primaria; el formato no cumple ninguna función de seguridad |
| `RazonSocial` | Texto | Sí | FR-001, FR-002: no vacía ni compuesta solo por espacios; FR-020: no única | Se conserva el valor tal como fue ingresado, sin normalizar |
| `Pais` | Texto (código ISO 3166-1) | Sí | FR-001, FR-003: debe pertenecer a la lista de países soberanos ISO 3166-1 | Validado contra un catálogo estático en Domain (`CatalogoPaisesIso3166`), sin dependencia de infraestructura |
| `IdentificadorFiscal` | Texto | Sí | FR-001, FR-004: no vacío | Se conserva el valor original tal como fue ingresado (para mostrarlo al usuario) |
| `IdentificadorFiscalNormalizado` | Texto (derivado) | Sí (calculado) | FR-010: sin espacios, en mayúsculas, sin separadores comunes (`-`, `.`, `/`) | Value object calculado a partir de `IdentificadorFiscal`; es el valor que participa en la unicidad, no se ingresa directamente |
| `NombreContacto` | Texto | Sí | FR-001, FR-005: no vacío | Se conserva el valor tal como fue ingresado |
| `CorreoContacto` | Texto | Sí | FR-001, FR-006: debe tener formato de correo electrónico válido | Solo se valida formato (ver Supuestos de spec.md); no se valida dominio ni unicidad |
| `Estado` | Enum `EstadoProveedor` | Sí | FR-012, FR-013: todo proveedor nuevo inicia en `Pendiente`, sin excepción | En el alcance de esta funcionalidad, el único valor posible es `Pendiente`; no se modelan transiciones |
| `RegistradoPor` | Texto/identificador de usuario | Sí | FR-015: quién realizó el registro | Obtenido en `Application` mediante el puerto `IUsuarioActual.IdentificadorUsuario` (ver research.md punto 11), nunca desde `RegistrarProveedorComando` ni desde el body de la solicitud HTTP; el cliente HTTP NO puede proporcionar ni modificar este valor. La autenticación/autorización concretas continúan fuera de alcance (FR-019); `Api` provee una implementación temporal basada en `ClaimsPrincipal`/`HttpContext` |
| `RegistradoEn` | Fecha/hora (UTC) | Sí | FR-015: fecha y hora de registro | `DateTimeOffset` en UTC, obtenido en `Application` mediante `TimeProvider` (no `DateTimeOffset.UtcNow` directo) y pasado como parámetro al crear el agregado; el dominio no accede a ningún reloj global |

### Reglas de validación (entrada) — FR-002 a FR-006

Evaluadas por `RegistrarProveedorValidador` en `Application`, **antes** de invocar el caso de uso
sobre el dominio (ver [ADR-0007](../../docs/adr/ADR-0007-decision-fluentvalidation.md)). Todas las
reglas se evalúan de forma independiente y se acumulan en una lista de errores (Caso Límite:
múltiples datos inválidos simultáneos):

1. `RazonSocial` no puede ser vacía ni contener únicamente espacios en blanco (FR-002).
2. `Pais` debe pertenecer al catálogo de países soberanos ISO 3166-1 (FR-003).
3. `IdentificadorFiscal` no puede ser vacío (FR-004).
4. `NombreContacto` no puede ser vacío (FR-005).
5. `CorreoContacto` debe tener un formato de correo electrónico válido (FR-006).

Estas mismas cinco reglas se protegen además, de forma independiente, como invariantes de guarda en
el constructor de `Proveedor` en `Domain`: si `RegistrarProveedorValidador` se omitiera o dejara
pasar algo por error, la entidad `Proveedor` igualmente rechaza (lanzando una excepción) su propia
construcción en un estado inválido. La validación de `Application` existe para acumular y devolver
al usuario todos los errores encontrados (FR-017); la guarda de `Domain` existe para que ningún
estado inválido pueda persistir sin depender de que `Application` la haya invocado correctamente.

### Invariantes de la entidad `Proveedor` (FR-010, FR-012, FR-013)

Reglas que la propia entidad puede garantizar de forma aislada, sin conocer a otros proveedores, y
que se mantienen fuera de la capa HTTP y de infraestructura (restricción explícita del plan):

1. `IdentificadorFiscalNormalizado` se calcula eliminando espacios en blanco, convirtiendo a
   mayúsculas y eliminando los separadores `-`, `.` y `/` del valor de `IdentificadorFiscal`
   (FR-010).
2. Todo proveedor nuevo se crea con `Estado = Pendiente` (FR-012) y ningún comportamiento del
   dominio permite construir un proveedor en otro estado inicial (FR-013).
3. El constructor de `Proveedor` rechaza (guard clauses) cualquier valor vacío o inválido de
   `RazonSocial`, `Pais`, `IdentificadorFiscal`, `NombreContacto` o `CorreoContacto`, como defensa
   adicional independiente de `RegistrarProveedorValidador` (ver párrafo anterior).

### Regla de negocio de unicidad país + identificador fiscal (FR-007, FR-008) — Application, no Domain

A diferencia de las reglas anteriores, la unicidad de `(Pais, IdentificadorFiscalNormalizado)`
**no** es una invariante que la entidad `Proveedor`, de forma aislada, pueda verificar por sí
misma: requiere conocer a otros proveedores ya registrados, algo fuera del alcance de una única
instancia de agregado. Por eso esta regla se clasifica como una regla de negocio *cross-aggregate*
y se aplica en dos niveles complementarios:

1. **`Application`** (`RegistrarProveedorCasoDeUso`): consulta
   `IProveedorRepository.ExisteAsync(pais, identificadorFiscalNormalizado)` antes de intentar
   registrar, para dar una respuesta de negocio clara en el camino feliz (FR-009).
2. **Persistencia** (`Infrastructure`): la tabla de proveedores mantiene además una restricción
   `UNIQUE` sobre `(Pais, IdentificadorFiscalNormalizado)` para sostener la regla ante condiciones
   de carrera (Caso Límite de concurrencia de `spec.md`). Si dos solicitudes concurrentes superan
   la verificación previa, la restricción de base de datos rechaza la segunda escritura;
   `ProveedorRepository` traduce esa violación en un resultado (`ConflictoDuplicado`), no en una
   excepción de dominio, que `RegistrarProveedorCasoDeUso` interpreta con el mismo resultado
   funcional que la verificación previa (rechazo por duplicado, FR-009/FR-018). Ver
   [ADR-0005](../../docs/adr/ADR-0005-estrategia-unicidad.md).

### Transiciones de Estado

Fuera del alcance de esta especificación (ver Supuestos de `spec.md`): no se modelan transiciones
desde `Pendiente` hacia ningún otro estado. `EstadoProveedor` se define como un enum que, en esta
funcionalidad, admite un único valor (`Pendiente`), dejando la extensión futura (por ejemplo,
`Aprobado`, `Rechazado`) para especificaciones independientes, tal como indican los Supuestos.

### Relaciones

Ninguna. `Proveedor` es una entidad independiente en el alcance de esta funcionalidad; no existen
relaciones con otras entidades (no se modela "Onboarding" ni entidades relacionadas, conforme a los
Supuestos de `spec.md`).

## Value Object: IdentificadorFiscalNormalizado

Encapsula la transformación de FR-010 como una operación pura del dominio, sin dependencias de
infraestructura:

- **Entrada**: el texto original de `IdentificadorFiscal`.
- **Transformación**: eliminar espacios en blanco → convertir a mayúsculas → eliminar `-`, `.`, `/`.
- **Salida**: texto normalizado, usado únicamente para comparar y para el índice único de base de
  datos; nunca se muestra al usuario en lugar del valor original.
- **Caso límite abierto (CHK029 del checklist de calidad)**: si tras normalizar el resultado queda
  vacío (por ejemplo, un identificador fiscal compuesto solo por separadores), este plan **no**
  inventa una regla de negocio no aprobada. Se documenta como comportamiento a confirmar por el
  negocio antes de la implementación; de no resolverse antes de `tasks.md`, la implementación debe
  tratarlo de forma explícita y trazable (por ejemplo, seguir tratándolo como valor no vacío desde
  FR-004, sin agregar una nueva regla de rechazo no solicitada) y señalarse en la revisión.

## Catálogo: CatalogoPaisesIso3166

Estructura estática en Domain (sin acceso a base de datos ni a servicios externos) que expone la
lista de códigos de país reconocidos como soberanos según ISO 3166-1, excluyendo territorios
dependientes, conforme a la clarificación registrada en `spec.md`. Se mantiene en Domain porque es
una regla de negocio (qué es un "país válido"), no un detalle de infraestructura.

## Enum: EstadoProveedor

```text
Pendiente
```

Único valor en el alcance de esta funcionalidad (FR-012, FR-013). No se agregan valores futuros
(`Aprobado`, `Rechazado`, `Inactivo`, etc.) porque están fuera de alcance según los Supuestos de
`spec.md`; agregar el enum completo ahora sin comportamiento asociado sería una anticipación no
solicitada.

## Índice único de persistencia (Infrastructure)

Ver [ADR-0005](../../docs/adr/ADR-0005-estrategia-unicidad.md). La configuración de EF Core para
`Proveedor` define un índice único sobre `(Pais, IdentificadorFiscalNormalizado)`. `IdentificadorFiscal`
(valor original) y `IdentificadorFiscalNormalizado` (valor derivado) se persisten como columnas
separadas: la primera para mostrar al usuario el valor tal como lo ingresó, la segunda únicamente
para sostener la unicidad y las búsquedas de duplicados.
