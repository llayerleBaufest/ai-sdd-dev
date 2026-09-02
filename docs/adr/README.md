# Registro de Decisiones Arquitectónicas (ADR)

Este directorio contiene las decisiones arquitectónicas significativas del proyecto
"AI-Native Supplier Onboarding", tal como exige el Principio I y las Restricciones de Ingeniería de
la [Constitution](../../.specify/memory/constitution.md).

Cada ADR documenta: problema, alternativas consideradas, decisión, justificación y trade-offs. Las
ADRs se numeran secuencialmente y no se eliminan; si una decisión se reemplaza, se crea una nueva
ADR que la reemplaza y se referencia mutuamente.

## Índice

| ADR | Título | Estado | Funcionalidad de origen |
|-----|--------|--------|--------------------------|
| [ADR-0001](./ADR-0001-aplicacion-unica-vs-microservicios.md) | Aplicación única frente a microservicios | Aceptada | 001-registrar-proveedor |
| [ADR-0002](./ADR-0002-separacion-de-proyectos.md) | Separación de proyectos por responsabilidad | Aceptada | 001-registrar-proveedor |
| [ADR-0003](./ADR-0003-mecanismo-persistencia.md) | Mecanismo de persistencia (EF Core 10 + SQL Server) | Aceptada | 001-registrar-proveedor |
| [ADR-0004](./ADR-0004-formato-identificador-interno.md) | Formato técnico del identificador interno de proveedor | Aceptada | 001-registrar-proveedor |
| [ADR-0005](./ADR-0005-estrategia-unicidad.md) | Estrategia de unicidad país + identificador fiscal | Aceptada | 001-registrar-proveedor |
| [ADR-0006](./ADR-0006-estrategia-pruebas-integracion.md) | Estrategia de pruebas de integración | Aceptada | 001-registrar-proveedor |
| [ADR-0007](./ADR-0007-decision-fluentvalidation.md) | Decisión sobre FluentValidation | Aceptada | 001-registrar-proveedor |
| [ADR-0008](./ADR-0008-estrategia-observabilidad.md) | Estrategia inicial de observabilidad | Aceptada | 001-registrar-proveedor |
