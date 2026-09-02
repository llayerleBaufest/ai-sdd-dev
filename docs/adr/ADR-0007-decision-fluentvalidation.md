# ADR-0007: Decisión sobre FluentValidation

**Estado**: Aceptada
**Fecha**: 2026-09-02
**Funcionalidad de origen**: [001-registrar-proveedor](../../specs/001-registrar-proveedor/spec.md)

## Contexto / Problema

FR-002, FR-003, FR-004, FR-005 y FR-006 exigen validar cinco datos de entrada (razón social, país,
identificador fiscal, nombre de contacto, correo electrónico). El Caso Límite de "múltiples errores
de validación simultáneos" exige que el usuario reciba información suficiente para identificar
**todos** los datos inválidos en un mismo intento, no solo el primero detectado. Se debe evaluar si
una biblioteca de validación declarativa (FluentValidation) aporta valor suficiente para justificar
su incorporación como dependencia.

## Alternativas Consideradas

1. **FluentValidation**: definir un `AbstractValidator<RegistrarProveedorComando>` declarativo que
   acumula automáticamente todos los fallos de regla por propiedad.
2. **Validación manual explícita** dentro de `Application` (`RegistrarProveedorValidador`): una
   clase que evalúa cada una de las cinco reglas y acumula una lista de errores (campo + mensaje),
   sin depender de ninguna biblioteca externa.

## Decisión

Se adopta la alternativa 2: **no se incorpora FluentValidation** en esta funcionalidad. Se
implementa un validador manual y explícito en `SupplierOnboarding.Application` que evalúa las cinco
reglas de entrada y devuelve la lista completa de errores encontrados.

## Justificación

- Las reglas actuales son simples y no tienen condicionales cruzados entre campos: cada una se
  reduce a "no vacío/no solo espacios", "formato de correo válido" o "pertenece al catálogo
  ISO 3166-1". Acumular una lista de errores con esta cantidad de reglas no requiere una biblioteca
  dedicada.
- El Caso Límite de múltiples errores simultáneos se satisface igual de bien recorriendo las cinco
  reglas y agregando cada fallo a una lista, sin la sobrecarga conceptual ni la dependencia externa
  que introduce FluentValidation.
- La restricción explícita de este plan y el Principio VIII de la Constitution exigen no incorporar
  tecnología, patrones o librerías "porque sean habituales"; cada elemento debe tener una razón
  concreta. Aquí no existe una razón concreta suficiente (no hay complejidad de reglas que
  FluentValidation resuelva mejor que un método simple).

## Trade-offs

- Si en el futuro se agregan más campos, reglas condicionales complejas, validación anidada de
  objetos o necesidad de reutilizar validadores entre múltiples casos de uso, esta decisión debe
  revisarse explícitamente (posible ADR de reemplazo) evaluando entonces si FluentValidation aporta
  valor suficiente para justificar la dependencia.
- La validación manual requiere disciplina para mantenerse legible a medida que crezca; se mitiga
  manteniendo `RegistrarProveedorValidador` como una única responsabilidad acotada (solo las cinco
  reglas de FR-002 a FR-006), sin mezclar validación de entrada con reglas de negocio de unicidad
  (que pertenecen al caso de uso, no al validador).
