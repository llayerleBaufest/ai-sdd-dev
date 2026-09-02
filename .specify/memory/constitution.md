<!--
Sync Impact Report
- Versión: (ninguna) → 1.0.0
- Tipo de cambio: Ratificación inicial (MAJOR)
- Principios definidos: 8 nuevos
  1. Intención de Negocio y Trazabilidad Primero
  2. Seguridad por Defecto
  3. Testeabilidad y Observabilidad por Defecto
  4. Responsabilidad Humana sobre la IA
  5. Simplicidad antes que Distribución
  6. Testabilidad como Requisito de Diseño
  7. Acceso a Datos mediante Repository Pattern
  8. Calidad, Simplicidad y Buenas Prácticas de Código
- Secciones agregadas:
  - Restricciones de Ingeniería, Tecnología y Entorno Azure
  - Flujo de Desarrollo
  - Governance (procedimiento de enmienda, versionado, cumplimiento)
- Secciones removidas: ninguna (documento inicial)
- Plantillas dependientes: no se modifican en este comando (spec-template, plan-template,
  tasks-template, checklist-template deben revisarse en la próxima iteración de cada uno
  para confirmar alineación con estos principios; no se detectaron referencias desactualizadas
  al momento de esta ratificación).
- Placeholders diferidos: ninguno.
-->

# AI-Native Supplier Onboarding Constitution

## Contexto del Proyecto

"AI-Native Supplier Onboarding" es una implementación de referencia, educativa y de calidad
empresarial, cuyo propósito es demostrar Spec-Driven Development, ingeniería de software asistida
por IA, .NET 10, Microsoft Azure y Microsoft Foundry. Toda la constitución y todos los artefactos
de gobierno del proyecto DEBEN redactarse en español.

## Principios Fundamentales

### I. Intención de Negocio y Trazabilidad Primero
Toda decisión de implementación DEBE poder rastrearse hasta un requerimiento de negocio, una
especificación, una decisión arquitectónica (ADR) o una restricción técnica aprobada. Las
especificaciones son la principal fuente de verdad sobre el comportamiento esperado del producto.
Todo cambio material de requerimientos DEBE reflejarse en la especificación correspondiente antes
de considerar completa su implementación. El código generado por IA, el código escrito
manualmente, las pruebas, la infraestructura y el comportamiento de componentes de IA DEBEN
mantenerse alineados con las especificaciones y decisiones aprobadas vigentes.
**Rationale**: sin trazabilidad explícita, el proyecto pierde su valor como referencia de
Spec-Driven Development y las decisiones se vuelven imposibles de auditar o justificar.

### II. Seguridad por Defecto
La seguridad DEBE considerarse desde el diseño y mantenerse durante toda la implementación, nunca
como una actividad posterior. Los secretos, claves, tokens y credenciales NUNCA DEBEN almacenarse
en el repositorio ni incorporarse directamente al código fuente. Las cargas de trabajo en Azure
DEBEN utilizar Microsoft Entra ID, Managed Identity y RBAC con mínimo privilegio siempre que el
servicio lo permita. Las entradas externas DEBEN validarse en los límites del sistema. Las
operaciones sensibles DEBEN estar autenticadas, autorizadas y ser auditables. Toda excepción de
seguridad DEBE documentarse y justificarse explícitamente antes de su implementación.
**Rationale**: los proveedores manejan datos sensibles de terceros; un incidente de seguridad
compromete tanto el objetivo educativo como la credibilidad de la referencia técnica.

### III. Testeabilidad y Observabilidad por Defecto
Todo comportamiento crítico de negocio DEBE contar con pruebas automatizadas apropiadas para su
nivel. Las reglas de dominio DEBEN cubrirse principalmente con pruebas unitarias. La persistencia,
la mensajería, la infraestructura y las integraciones externas DEBEN validarse mediante pruebas de
integración o de contrato cuando corresponda. Las cargas de trabajo productivas DEBEN proporcionar
logs estructurados, métricas y trazas distribuidas suficientes para diagnosticar fallos y
comprender el comportamiento del sistema. La observabilidad es parte de la arquitectura y DEBE
diseñarse junto con la funcionalidad, no agregarse posteriormente. Se DEBEN preferir mecanismos
compatibles con OpenTelemetry cuando resulte técnicamente adecuado.
**Rationale**: sin pruebas y observabilidad integradas desde el diseño, el sistema no puede
evolucionar con confianza ni servir como ejemplo de calidad empresarial.

### IV. Responsabilidad Humana sobre la IA
La IA PUEDE extraer, clasificar, resumir, detectar inconsistencias, analizar información y
recomendar acciones, pero NO DEBE tomar decisiones de negocio irreversibles o de alto impacto
salvo que una especificación lo autorice explícitamente. La aprobación o el rechazo final de
proveedores REQUIERE, inicialmente, confirmación humana explícita. Las reglas determinísticas
DEBEN mantenerse en lógica de aplicación cuando puedan expresarse de forma confiable sin IA
generativa. El comportamiento de la IA DEBE poder probarse y evaluarse mediante escenarios
representativos. Los cambios en modelos, prompts, herramientas o workflows de IA que alteren
materialmente el comportamiento DEBEN ser trazables. Una falla, respuesta inválida o comportamiento
inesperado de un modelo de IA NUNCA DEBE comprometer la consistencia del dominio ni producir
decisiones definitivas sin control.
**Rationale**: el proyecto demuestra IA aplicada de forma responsable; la autonomía de la IA debe
crecer solo cuando existan especificaciones y evidencia que lo justifiquen.

### V. Simplicidad antes que Distribución
La arquitectura DEBE comenzar con la solución más simple capaz de satisfacer los requerimientos
documentados y los atributos de calidad exigidos. NO DEBEN introducirse microservicios, mensajería
asincrónica, componentes distribuidos o servicios de Azure adicionales únicamente porque estén
disponibles. Todo componente arquitectónico relevante DEBE tener una razón documentada para
existir. Los límites entre servicios y la elección entre comunicación síncrona y asincrónica DEBEN
justificarse por necesidades concretas de negocio, escalabilidad, confiabilidad, seguridad,
ownership, independencia de despliegue o integración. Los patrones distribuidos solo DEBEN
introducirse cuando el beneficio justifique su complejidad técnica y operativa.
**Rationale**: la complejidad distribuida no justificada dificulta el aprendizaje, incrementa el
costo y el riesgo, y contradice el propósito educativo del proyecto.

### VI. Testabilidad como Requisito de Diseño
Todo componente que contenga lógica de negocio o comportamiento relevante DEBE diseñarse para
poder probarse de manera aislada mediante pruebas unitarias. Las dependencias externas, de
infraestructura o reemplazables DEBEN exponerse mediante abstracciones cuando esto permita
desacoplar la lógica, sustituir implementaciones y facilitar las pruebas. Se DEBEN utilizar
interfaces especialmente para servicios de aplicación, repositorios, gateways, clientes externos y
otros servicios de infraestructura reemplazables. NO DEBEN crearse interfaces de forma mecánica
para cada clase si no existe una necesidad real de abstracción, desacoplamiento, sustitución o
testabilidad. Las pruebas unitarias DEBEN ser rápidas, determinísticas e independientes de bases de
datos, red, sistema de archivos, Azure y otros componentes de infraestructura. Las dependencias de
infraestructura DEBEN validarse mediante pruebas de integración cuando corresponda. Todo
comportamiento relevante de negocio DEBE contar con cobertura automatizada; el objetivo no es
perseguir un porcentaje arbitrario de cobertura, sino asegurar que las reglas, decisiones y
comportamientos importantes estén correctamente protegidos por pruebas.
**Rationale**: diseñar para la testabilidad desde el inicio evita reescrituras costosas y permite
mantener la confiabilidad del sistema a medida que crece.

### VII. Acceso a Datos mediante Repository Pattern
El acceso a datos persistentes DEBE realizarse mediante el patrón Repository. La lógica de
aplicación y de dominio NO DEBE depender directamente de Entity Framework Core, DbContext, SQL ni
de tecnologías concretas de persistencia. Los repositorios DEBEN expresar operaciones relacionadas
con necesidades reales del dominio. DEBE evitarse crear repositorios genéricos que únicamente
repliquen de forma mecánica operaciones CRUD del ORM sin aportar una abstracción útil. Los
contratos de los repositorios DEBEN exponerse mediante interfaces; las implementaciones concretas
DEBEN permanecer en la capa de infraestructura. La tecnología de persistencia DEBE poder cambiar
sin afectar las reglas de negocio ni los casos de uso. Las pruebas unitarias de dominio y
aplicación NO DEBEN requerir una base de datos real.
**Rationale**: es una decisión obligatoria del proyecto que garantiza desacoplamiento de la
persistencia y sostiene el principio de testabilidad sin infraestructura real.

### VIII. Calidad, Simplicidad y Buenas Prácticas de Código
Todo código DEBE cumplir principios de legibilidad, mantenibilidad, cohesión, bajo acoplamiento y
responsabilidad clara. DEBEN aplicarse buenas prácticas de desarrollo y los principios SOLID
cuando resulten pertinentes. Los patrones de diseño DEBEN utilizarse únicamente cuando resuelvan un
problema concreto o mejoren de manera demostrable la mantenibilidad, extensibilidad, testabilidad,
desacoplamiento o claridad del diseño. NO DEBEN introducirse patrones únicamente para demostrar
conocimiento técnico, seguir modas o anticipar necesidades que todavía no existen. Entre una
solución compleja basada en patrones y una solución simple que satisface correctamente los
requerimientos, DEBE preferirse la solución simple mientras no comprometa atributos de calidad
relevantes. Las abstracciones DEBEN surgir de necesidades concretas del diseño. DEBE evitarse:
código duplicado; responsabilidades mezcladas; dependencias ocultas; estado global mutable;
métodos o clases excesivamente grandes; acoplamiento innecesario; abstracciones prematuras;
sobreingeniería; lógica de negocio dentro de controladores, infraestructura o componentes de
presentación. La implementación DEBE favorecer código comprensible por otro desarrollador sin
depender de conocimiento implícito del autor.
**Rationale**: la calidad de código es un atributo verificable en revisión y sostiene la
mantenibilidad a largo plazo del proyecto como referencia técnica.

## Restricciones de Ingeniería, Tecnología y Entorno Azure

**Plataforma y tecnología**
- La plataforma objetivo DEBE ser .NET 10 y Microsoft Azure.
- Microsoft Foundry DEBE utilizarse únicamente cuando una necesidad funcional o técnica justifique
  capacidades de IA.
- Los servicios de Azure DEBEN incorporarse porque resuelven requerimientos identificados y no
  únicamente con fines demostrativos.
- La infraestructura cloud DEBE poder reproducirse mediante Infrastructure as Code a medida que el
  proyecto madure.
- DEBEN preferirse Managed Identities frente a credenciales almacenadas siempre que el servicio lo
  permita.
- El desarrollo asistido por IA NUNCA DEBE omitir revisión de código, pruebas, seguridad ni
  gobierno arquitectónico.
- Las decisiones arquitectónicas significativas DEBEN registrarse explícitamente, preferentemente
  mediante ADRs cuando corresponda.
- La mantenibilidad, comprensibilidad y testabilidad son atributos obligatorios de la arquitectura,
  la cual DEBE justificarse por los requerimientos.
- NO DEBEN imponerse por defecto microservicios, CQRS, Clean Architecture u otros patrones o
  estilos arquitectónicos si no existe una necesidad que los justifique.
- El Repository Pattern SÍ constituye una decisión obligatoria del proyecto para el acceso a
  persistencia (véase Principio VII).
- Las interfaces DEBEN utilizarse de forma intencional para desacoplar dependencias y facilitar
  pruebas, no como regla mecánica de una interfaz por clase.

**Entorno y suscripción de Azure**
- La suscripción de Azure utilizada por el proyecto es compartida con otros usuarios.
- Todos los recursos Azure pertenecientes a este proyecto DEBEN crearse dentro del Resource Group
  `rg-llayerle-ai-sdd-dev`.
- NO DEBEN modificarse, reutilizarse ni eliminarse recursos Azure compartidos o pertenecientes a
  otros usuarios.
- NO DEBEN crearse recursos del proyecto fuera de `rg-llayerle-ai-sdd-dev`, salvo que exista una
  limitación técnica real que lo requiera; cualquier excepción DEBE documentarse y justificarse
  explícitamente antes de crear el recurso.
- Los recursos Azure del proyecto DEBEN utilizar tags que permitan identificar propietario,
  proyecto, ambiente y propósito cuando el servicio lo permita.
- DEBE evitarse crear infraestructura manual innecesaria; a medida que el proyecto madure, la
  infraestructura DEBERÁ administrarse mediante Infrastructure as Code.
- Antes de crear un nuevo servicio Azure DEBE existir una necesidad funcional, técnica o educativa
  documentada que justifique su incorporación.
- NO DEBEN realizarse pruebas sobre recursos compartidos existentes.
- Toda automatización o script que cree, modifique o elimine recursos Azure DEBE limitar
  explícitamente su alcance a los recursos pertenecientes a este proyecto.

## Flujo de Desarrollo

El flujo de trabajo obligatorio del proyecto es:

Especificación → Aclaración → Checklist → Planificación → Tareas → Análisis → Implementación →
Verificación.

- Las ambigüedades importantes DEBEN resolverse antes de implementar.
- Toda funcionalidad DEBE poder rastrearse desde el requerimiento hasta la implementación y sus
  pruebas.
- Una implementación que compila pero viola la especificación, la constitución o los criterios de
  aceptación DEBE considerarse incorrecta, independientemente de que el build sea exitoso.

## Governance

Esta constitución prevalece sobre cualquier otra práctica, guía, convención o preferencia individual
en caso de conflicto. Toda revisión de código y de artefactos de planificación DEBE verificar el
cumplimiento de los principios aquí definidos; la complejidad no justificada DEBE rechazarse o
justificarse explícitamente antes de aceptarse.

**Procedimiento de enmienda**: los cambios a esta constitución DEBEN ser explícitos, versionados y
registrados mediante un Sync Impact Report al inicio del documento. Toda propuesta de enmienda
DEBE identificar los principios afectados y las especificaciones, planes o decisiones
arquitectónicas (ADRs) que requieran revisión como consecuencia del cambio.

**Política de versionado**: esta constitución sigue versionado semántico (MAJOR.MINOR.PATCH):
- MAJOR: eliminación o redefinición incompatible de principios o reglas de gobierno existentes.
- MINOR: adición de un nuevo principio o sección, o ampliación material de una guía existente.
- PATCH: aclaraciones, correcciones de redacción o refinamientos no semánticos.

**Revisión de cumplimiento**: los cambios materiales a los principios DEBEN acompañarse de una
revisión de las especificaciones, planes y decisiones arquitectónicas afectadas para confirmar que
permanecen alineadas con la constitución vigente.

**Version**: 1.0.0 | **Ratified**: 2026-09-02 | **Last Amended**: 2026-09-02
