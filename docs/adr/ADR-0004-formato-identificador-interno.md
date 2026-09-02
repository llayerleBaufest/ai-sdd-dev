# ADR-0004: Formato técnico del identificador interno de proveedor

**Estado**: Aceptada
**Fecha**: 2026-09-02
**Funcionalidad de origen**: [001-registrar-proveedor](../../specs/001-registrar-proveedor/spec.md)

## Contexto / Problema

FR-014 exige que cada proveedor reciba una identidad propia y única dentro del sistema, pero
declara explícitamente que la especificación funcional no impone ningún formato concreto, ya que su
representación técnica corresponde a la fase de planificación, y que ningún comportamiento
funcional debe depender de dicho formato. Corresponde a este plan elegir una representación técnica
concreta.

## Alternativas Consideradas

1. **Entero autoincremental** (`int`/`bigint` IDENTITY de SQL Server).
2. **GUID aleatorio (UUID v4)**, generado con `Guid.NewGuid()`.
3. **GUID ordenado en el tiempo (UUID v7)**, generado con `Guid.CreateVersion7()` (disponible de
   forma nativa en .NET desde la versión 9, incluida en .NET 10).
4. **ULID** mediante una biblioteca de terceros.

## Decisión

Se adopta la alternativa 3: **GUID versión 7**, generado en el dominio al crear la entidad
`Proveedor`, mediante `Guid.CreateVersion7(instante)`, recibiendo el instante de registro como
parámetro (ver nota sobre `TimeProvider` más abajo) en lugar de leer un reloj global.

## Justificación

- Un entero autoincremental requiere que la base de datos asigne el valor al insertar, lo que
  impide construir y asignar la identidad del agregado en `Domain` antes de persistir, dificultando
  las pruebas unitarias sin infraestructura real (Principios VI y VII de la Constitution).
- Un GUID v4 aleatorio puede generarse en el dominio sin depender de la base de datos, pero al
  insertarse en un índice clúster de SQL Server genera fragmentación de página significativa por su
  falta de orden, degradando el rendimiento de escritura/índices a medida que crece la tabla.
- GUID v7 combina lo mejor de ambos: puede generarse íntegramente en el dominio (sin round-trip a
  la base de datos) y conserva orden temporal aproximado (los primeros bits codifican un
  timestamp), lo que reduce la fragmentación de índice frente a GUID v4 puro, sin requerir
  dependencias externas (soporte nativo de .NET 10). Los beneficios de esta elección son de
  **unicidad, generación distribuida sin coordinación central y orden temporal favorable para la
  persistencia**.
- Este identificador **no cumple ninguna función de seguridad ni de autorización**: ninguna regla
  de negocio o control de acceso de este proyecto depende de que el identificador sea difícil de
  predecir. Cualquier control de acceso sobre el recurso proveedor debe basarse en autenticación y
  autorización explícitas (fuera de alcance de esta funcionalidad, FR-019), nunca en la opacidad
  del identificador.
- Recibir el instante como parámetro (en vez de invocar `DateTimeOffset.UtcNow`/`DateTime.Now`
  dentro de `Domain`) permite generar el mismo GUID de forma determinística en pruebas: `Domain` no
  depende de ningún reloj global; `Application` obtiene el instante mediante `TimeProvider` y lo
  pasa explícitamente al crear el `Proveedor`.
- Ninguna regla de negocio de `spec.md` depende del formato del identificador (tal como exige
  FR-014); esta decisión es puramente técnica y reversible mediante una nueva ADR si cambian las
  condiciones.

## Trade-offs

- Un GUID (16 bytes) ocupa más espacio que un `int`/`bigint` (4/8 bytes) como clave primaria e
  índice único, con un costo marginal de almacenamiento e I/O.
- La parte temporal de un GUID v7 revela aproximadamente el momento de creación del registro; no se
  considera información sensible en este dominio (no es un dato de negocio protegido) y no expone
  el orden relativo de inserción con la misma precisión que un entero secuencial.
- Si en el futuro se requiere optimizar al máximo el tamaño de índice o exponer IDs cortos legibles
  por humanos, esta decisión deberá revisarse explícitamente sin alterar reglas de negocio
  existentes, dado que FR-014 ya garantiza esa libertad.
