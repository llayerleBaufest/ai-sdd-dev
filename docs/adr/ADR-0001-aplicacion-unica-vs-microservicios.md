# ADR-0001: Aplicación única frente a microservicios

**Estado**: Aceptada
**Fecha**: 2026-09-02
**Funcionalidad de origen**: [001-registrar-proveedor](../../specs/001-registrar-proveedor/spec.md)

## Contexto / Problema

Se debe decidir el estilo arquitectónico inicial para implementar "Registrar proveedor": ¿una única
aplicación desplegable o una descomposición en microservicios? La especificación no define ningún
requisito de escalabilidad independiente, despliegue independiente por equipo, ni aislamiento de
fallos entre subdominios.

## Alternativas Consideradas

1. **Microservicios** separados por bounded context (por ejemplo, un servicio de "Proveedores" y
   otro futuro de "Onboarding"), comunicados vía HTTP o mensajería asíncrona.
2. **Monolito modular**: una única aplicación desplegable, internamente separada en proyectos con
   responsabilidades claras (dominio, casos de uso, infraestructura, HTTP).
3. **Monolito no estructurado**: una única aplicación sin separación interna de responsabilidades.

## Decisión

Se adopta la alternativa 2: **monolito modular**, desplegado como una única aplicación ASP.NET
Core (`SupplierOnboarding.Api`), internamente separada en los proyectos descritos en
`plan.md` (Domain, Application, Infrastructure, Api).

## Justificación

- El Principio V de la Constitution ("Simplicidad antes que Distribución") exige comenzar con la
  solución más simple capaz de satisfacer los requisitos documentados, y prohíbe introducir
  microservicios sin una necesidad concreta de negocio, escalabilidad, confiabilidad, seguridad,
  ownership o independencia de despliegue.
- La especificación actual describe un único caso de uso (`RegistrarProveedor`) sin requisitos de
  volumen, carga masiva ni escalado diferenciado (ver sección Supuestos de `spec.md`).
- No existe ningún equipo, dominio de negocio adicional ni necesidad de despliegue independiente
  que justifique el costo operativo de una arquitectura distribuida (red, serialización,
  consistencia eventual, observabilidad distribuida adicional).
- El monolito modular no descarta evolucionar hacia servicios separados en el futuro: la separación
  interna en proyectos (ADR-0002) ya aísla el dominio de la infraestructura, facilitando una
  eventual extracción si un requisito futuro lo justifica.

## Trade-offs

- Se posterga la posibilidad de escalar o desplegar el módulo de proveedores de forma
  independiente de otros módulos futuros; esto es aceptable porque hoy no existe ese módulo
  adicional ni ese requisito.
- Toda la aplicación comparte el mismo ciclo de despliegue; para el alcance actual (una sola
  funcionalidad) esto no introduce riesgo adicional relevante.
- Si en el futuro aparecen requisitos concretos (por ejemplo, escalado independiente de un
  subdominio de IA/documentos), esta decisión deberá revisarse mediante una nueva ADR que la
  reemplace, evaluando entonces la extracción de servicios sobre los límites ya definidos por
  Domain/Application.
