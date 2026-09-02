# ADR-0005: Estrategia de unicidad país + identificador fiscal

**Estado**: Aceptada
**Fecha**: 2026-09-02
**Funcionalidad de origen**: [001-registrar-proveedor](../../specs/001-registrar-proveedor/spec.md)

## Contexto / Problema

FR-007, FR-008 y FR-010 exigen que no puedan existir dos proveedores con la misma combinación de
país + identificador fiscal normalizado, sin excepción. Esta regla **no** es una invariante que la
entidad `Proveedor`, de forma aislada, pueda verificar por sí misma: requiere conocer a otros
proveedores ya registrados, algo fuera del alcance de una única instancia de agregado en `Domain`.
Se clasifica, por lo tanto, como una regla de negocio *cross-aggregate* que corresponde aplicar en
`Application` (con apoyo de `IProveedorRepository`) y reforzar en persistencia. Los Casos Límite de
la especificación exigen explícitamente que, ante dos intentos de registro concurrentes con la
misma combinación, solo uno se complete exitosamente y el otro se rechace informando la existencia
previa. Se debe decidir cómo garantizar esta invariante de forma segura ante condiciones de carrera.

## Alternativas Consideradas

1. **Verificación previa en código únicamente** (`existe combinación → rechazar; si no, insertar`),
   sin ninguna restricción a nivel de base de datos.
2. **Verificación previa en código + restricción única a nivel de base de datos**, traduciendo la
   violación de dicha restricción a un resultado funcional cuando dos inserciones concurrentes
   superan la verificación previa.
3. **Bloqueo pesimista o transacciones `SERIALIZABLE`** para serializar todos los registros de
   proveedores.

## Decisión

Se adopta la alternativa 2: el caso de uso `RegistrarProveedorCasoDeUso` (en `Application`) primero
consulta `IProveedorRepository.ExisteAsync(pais, identificadorFiscalNormalizado)` para dar una
respuesta de negocio rápida y clara en el camino feliz (FR-009); adicionalmente, la tabla de
proveedores define un **índice único** en `(Pais, IdentificadorFiscalNormalizado)` a nivel de base
de datos. Si dos solicitudes concurrentes superan la verificación previa y ambas intentan insertar,
la base de datos rechaza la segunda escritura mediante la restricción única; `ProveedorRepository`
(en `Infrastructure`) captura esa excepción de base de datos y la traduce a un **resultado
funcional** (`ResultadoAlmacenamientoProveedor.ConflictoDuplicado`) — **no** a una excepción de
dominio — que `RegistrarProveedorCasoDeUso` ya sabe interpretar con el mismo resultado observable
que la verificación previa: rechazo por duplicado (FR-009, FR-018). No se define una excepción de
duplicado en `Domain`, porque la entidad `Proveedor`, aislada, no puede detectar esa condición por
sí misma.

## Justificación

- La verificación previa por sí sola (alternativa 1) no es segura ante condiciones de carrera: dos
  solicitudes concurrentes pueden superar ambas la verificación antes de que cualquiera complete su
  inserción, violando FR-008 y el Caso Límite de concurrencia de `spec.md`.
- Un índice único en la base de datos es la única garantía verdaderamente atómica de unicidad,
  aplicada por el motor de SQL Server independientemente de la lógica de aplicación.
- Mantener la verificación previa en código conserva un mensaje de negocio claro y rápido en el
  camino feliz (evita depender siempre de una excepción de base de datos para el caso común), pero
  la restricción única en base de datos es la que realmente sostiene la invariante bajo
  concurrencia.
- Traducir la excepción de base de datos a un **resultado funcional** (no a una excepción) en
  `Infrastructure` evita usar excepciones para controlar el flujo de un desenlace de negocio
  habitual (rechazo por duplicado, FR-009), mantiene a `Application` y `Domain` libres de detalles
  de EF Core/SQL Server (Principio VII), y evita ubicar en `Domain` una excepción sobre una
  condición que la entidad aislada no puede detectar por sí misma.

## Trade-offs

- Se requiere manejar y traducir explícitamente una excepción específica de EF Core/SQL Server
  (violación de índice único) dentro de `ProveedorRepository`, convirtiéndola en el resultado
  `ConflictoDuplicado` en lugar de propagarla.
- Comparado con `SERIALIZABLE`/bloqueo pesimista, esta estrategia no serializa todas las escrituras
  de la tabla (menor contención), pero exige que el índice único esté correctamente definido sobre
  el valor ya normalizado (`IdentificadorFiscalNormalizado`), no sobre el valor original ingresado
  por el usuario.
- Este enfoque exige que el caso de uso trate el "camino feliz con verificación previa" y el
  "camino de conflicto detectado por la base de datos" como dos rutas que terminan en el mismo
  resultado observable (rechazo por duplicado, FR-009/FR-018), lo cual se cubre con pruebas de
  integración específicas (ADR-0006) además de pruebas unitarias del caso de uso.
