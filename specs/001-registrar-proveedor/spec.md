# Especificación de Funcionalidad: Registrar Proveedor

**Rama de Funcionalidad**: `001-registrar-proveedor`

**Creada**: 2026-09-02

**Estado**: Borrador

**Entrada**: Descripción de usuario: "Crear la especificación funcional para la funcionalidad 'Registrar proveedor' del proyecto AI-Native Supplier Onboarding. Un usuario interno autorizado debe poder registrar un nuevo proveedor para iniciar su proceso de onboarding dentro de la organización. Registrar un proveedor no significa aprobarlo; todo proveedor nuevo debe comenzar en estado Pendiente."

## Clarificaciones

### Sesión 2026-09-02

- P: ¿Qué criterio exacto define que un país sea "válido o soportado" para el registro de un
  proveedor? → R: Una lista basada en el estándar ISO 3166-1 (países reconocidos
  internacionalmente como entidades soberanas), sin incluir territorios dependientes.
- P: ¿Qué transformación exacta debe aplicarse al identificador fiscal antes de comparar
  duplicados dentro del mismo país? → R: Eliminar espacios en blanco, convertir a mayúsculas y
  eliminar separadores comunes (guiones, puntos, barras) antes de comparar el resultado como
  texto plano.
- P: ¿Puede existir más de un proveedor registrado con la misma razón social (nombre legal)? → R:
  Sí, la razón social puede repetirse entre proveedores distintos; solo la combinación país +
  identificador fiscal debe ser única.
- P: Además de tener un formato válido, ¿existe alguna restricción adicional sobre el correo
  electrónico de contacto? → R: No; únicamente se valida el formato del correo electrónico, sin
  restricciones de dominio ni de unicidad.
- P: Cuando el registro se rechaza por tratarse de un proveedor duplicado, ¿qué información
  exacta debe recibir el usuario además de indicar que ya existe un proveedor con esa
  combinación? → R: Solo el hecho de que ya existe un proveedor con esa combinación de país e
  identificador fiscal, sin exponer su identidad interna, razón social ni estado.
- P: ¿Debe la especificación definir un formato concreto para el identificador propio asignado al
  proveedor, o exigir que sea visible/comunicado al usuario? → R: No. La especificación funcional
  no impone ningún formato concreto para dicho identificador, ya que su representación técnica
  corresponde a la fase de planificación; el identificador puede mostrarse o utilizarse por el
  usuario cuando sea necesario para referenciar inequívocamente al proveedor, pero ningún
  comportamiento funcional depende de su formato o estructura interna.
- P: ¿La regla de unicidad de país + identificador fiscal debe seguir aplicando sin excepción si,
  en el futuro, un proveedor existente cambia de estado (por ejemplo, queda rechazado, inactivo o
  suspendido)? → R: Sí. La unicidad de país + identificador fiscal se mantiene sin excepción,
  independientemente del estado actual o futuro del proveedor. Los procesos futuros que permitan
  reiniciar un onboarding, reactivar un proveedor o modificar su estado deberán operar sobre la
  identidad ya existente del proveedor y se definirán mediante especificaciones independientes;
  esta especificación no introduce un modelo de "Onboarding", historial de onboarding, nuevos
  estados ni decisiones de arquitectura al respecto.

## Escenarios de Usuario y Pruebas *(obligatorio)*

### Historia de Usuario 1 - Registro exitoso de un nuevo proveedor (Prioridad: P1)

Como usuario interno autorizado, quiero registrar un nuevo proveedor proporcionando su información
básica, para iniciar su proceso de onboarding dentro de la organización.

**Por qué esta prioridad**: Es la funcionalidad central sin la cual no existe punto de entrada al
proceso de onboarding de proveedores. Sin esta capacidad, ninguna otra funcionalidad posterior
(revisión, aprobación, rechazo) tiene sentido.

**Prueba independiente**: Puede probarse completamente proporcionando razón social, país,
identificador fiscal, nombre de contacto y correo electrónico de contacto válidos, y verificando
que el proveedor queda registrado, en estado Pendiente, con una identidad propia y con la
trazabilidad de quién y cuándo lo registró.

**Escenarios de Aceptación**:

1. **Dado** que un usuario autorizado proporciona razón social, país válido, identificador fiscal,
   nombre de contacto y correo electrónico de contacto válidos, y no existe previamente un
   proveedor con esa combinación de país e identificador fiscal, **Cuando** el usuario envía el
   registro, **Entonces** el proveedor queda registrado, recibe una identidad propia, queda en
   estado "Pendiente", se conserva quién y cuándo fue registrado, y el usuario recibe confirmación
   de que el registro fue exitoso.
2. **Dado** que un proveedor fue registrado exitosamente, **Cuando** se consulta su información,
   **Entonces** su estado es "Pendiente" y no otro estado.
3. **Dado** que un proveedor fue registrado exitosamente, **Cuando** se consulta su información de
   auditoría, **Entonces** se puede identificar quién realizó el registro y en qué fecha y hora.
4. **Dado** que ya existe un proveedor registrado en un país con un identificador fiscal
   determinado, **Cuando** un usuario registra un proveedor distinto en un país diferente que
   posee el mismo valor textual de identificador fiscal, **Entonces** el nuevo proveedor se
   registra exitosamente como una entidad independiente.

---

### Historia de Usuario 2 - Rechazo de registro por datos obligatorios inválidos (Prioridad: P2)

Como usuario interno autorizado, quiero que el sistema me indique claramente qué datos son
inválidos cuando intento registrar un proveedor con información incompleta o incorrecta, para
poder corregirlos y completar el registro correctamente.

**Por qué esta prioridad**: Proteger la integridad de los datos de proveedores es esencial antes de
que cualquier proceso de onboarding posterior pueda confiar en dicha información; sin esta
validación, se podrían crear proveedores con datos inutilizables.

**Prueba independiente**: Puede probarse intentando registrar proveedores con cada dato
obligatorio faltante o inválido (razón social vacía, identificador fiscal vacío, correo inválido,
país no soportado, nombre de contacto vacío) y verificando que el registro se rechaza en cada caso
y que el usuario recibe información sobre qué corregir.

**Escenarios de Aceptación**:

1. **Dado** que un usuario intenta registrar un proveedor sin razón social (vacía o compuesta
   solamente por espacios), **Cuando** envía el registro, **Entonces** el proveedor no se registra
   y el usuario recibe información indicando que la razón social es inválida.
2. **Dado** que un usuario intenta registrar un proveedor sin identificador fiscal, **Cuando**
   envía el registro, **Entonces** el proveedor no se registra y el usuario recibe información
   indicando que el identificador fiscal es inválido.
3. **Dado** que un usuario intenta registrar un proveedor con un correo electrónico de contacto con
   formato inválido, **Cuando** envía el registro, **Entonces** el proveedor no se registra y el
   usuario recibe información indicando que el correo electrónico es inválido.
4. **Dado** que un usuario intenta registrar un proveedor con un país inválido o no soportado por
   el sistema, **Cuando** envía el registro, **Entonces** el proveedor no se registra y el usuario
   recibe información indicando que el país es inválido.
5. **Dado** que un usuario intenta registrar un proveedor sin nombre de contacto, **Cuando** envía
   el registro, **Entonces** el proveedor no se registra y el usuario recibe información indicando
   que el nombre de contacto es inválido.

---

### Historia de Usuario 3 - Rechazo de registro de un proveedor duplicado (Prioridad: P3)

Como usuario interno autorizado, quiero que el sistema impida registrar un proveedor que ya existe
para el mismo país e identificador fiscal, para evitar duplicados que generen confusión durante el
onboarding.

**Por qué esta prioridad**: Evita inconsistencias de datos y procesos de onboarding duplicados para
el mismo proveedor real; depende de que el registro básico (Historia 1) ya exista para poder
compararse contra proveedores previos.

**Prueba independiente**: Puede probarse registrando primero un proveedor válido y luego intentando
registrar otro con la misma combinación de país e identificador fiscal (incluyendo variantes de
formato irrelevantes), verificando que el segundo intento se rechaza y que el usuario es informado
de la existencia previa.

**Escenarios de Aceptación**:

1. **Dado** que ya existe un proveedor registrado con un país e identificador fiscal determinados,
   **Cuando** un usuario intenta registrar otro proveedor con exactamente la misma combinación de
   país e identificador fiscal, **Entonces** el nuevo registro se rechaza y el usuario recibe
   información suficiente para comprender que ya existe un proveedor con esa identificación.
2. **Dado** que ya existe un proveedor registrado con un identificador fiscal determinado en un
   país, **Cuando** un usuario intenta registrar otro proveedor en el mismo país con un
   identificador fiscal que difiere únicamente en diferencias de formato irrelevantes (por ejemplo
   espacios adicionales, mayúsculas/minúsculas o separadores) que no alteran su significado,
   **Entonces** el nuevo registro se rechaza por ser considerado el mismo proveedor.

---

### Casos Límite

- ¿Qué sucede si un usuario reintenta registrar exactamente el mismo proveedor luego de haber
  recibido previamente un rechazo por duplicado? El resultado debe seguir siendo un rechazo por
  duplicado.
- ¿Qué sucede si la razón social contiene solamente espacios en blanco? Debe tratarse como
  equivalente a un valor vacío y rechazarse.
- ¿Qué sucede si dos usuarios intentan registrar de forma simultánea un proveedor con la misma
  combinación de país e identificador fiscal? Solo uno de los registros debe completarse
  exitosamente; el otro debe rechazarse informando la existencia previa.
- ¿Qué sucede si se proporciona más de un dato obligatorio inválido en el mismo intento de
  registro? El usuario debe recibir información suficiente para identificar todos los datos que
  deben corregirse, no solo el primero detectado.

## Requisitos *(obligatorio)*

### Requisitos Funcionales

- **FR-001**: El sistema DEBE permitir a un usuario interno autorizado registrar un nuevo
  proveedor proporcionando como mínimo: razón social, país, identificador fiscal, nombre de
  contacto y correo electrónico de contacto.
- **FR-002**: El sistema DEBE rechazar el registro si la razón social está vacía o está compuesta
  únicamente por espacios en blanco.
- **FR-003**: El sistema DEBE rechazar el registro si el país proporcionado no corresponde a un
  país válido soportado por el sistema. Un país se considera válido cuando corresponde a un país
  reconocido internacionalmente según el estándar ISO 3166-1, excluyendo territorios dependientes
  y entidades no soberanas.
- **FR-004**: El sistema DEBE rechazar el registro si el identificador fiscal está vacío.
- **FR-005**: El sistema DEBE rechazar el registro si el nombre de la persona de contacto está
  vacío.
- **FR-006**: El sistema DEBE rechazar el registro si el correo electrónico de contacto no tiene un
  formato válido.
- **FR-007**: El sistema DEBE identificar de manera única a cada proveedor mediante la combinación
  de país e identificador fiscal.
- **FR-008**: El sistema NO DEBE permitir la existencia de dos proveedores con la misma combinación
  de país e identificador fiscal. Esta regla de unicidad DEBE mantenerse sin excepción,
  independientemente del estado actual o futuro del proveedor existente (por ejemplo, si dicho
  proveedor se encuentra Pendiente, Rechazado, Inactivo, Suspendido o en cualquier otro estado).
- **FR-009**: Cuando un usuario intente registrar un proveedor cuya combinación de país e
  identificador fiscal ya exista, el sistema DEBE rechazar el registro e informar al usuario que ya
  existe un proveedor con esa identificación.
- **FR-010**: La detección de proveedores duplicados DEBE considerar equivalentes a
  identificadores fiscales que difieran únicamente en diferencias de formato irrelevantes. Para
  ello, antes de comparar dos identificadores fiscales del mismo país, el sistema DEBE eliminar
  los espacios en blanco, convertir el texto a mayúsculas y eliminar los separadores comunes
  (guiones, puntos y barras), comparando luego el resultado como texto plano.
- **FR-011**: El sistema DEBE permitir el registro de proveedores en países diferentes que posean el
  mismo valor textual de identificador fiscal, siempre que la combinación de país e identificador
  fiscal sea distinta entre ellos.
- **FR-012**: Todo proveedor registrado exitosamente DEBE quedar, de forma automática, en estado
  "Pendiente".
- **FR-013**: El sistema NO DEBE permitir que un proveedor nuevo comience en un estado distinto de
  "Pendiente" (por ejemplo, aprobado o rechazado).
- **FR-014**: El sistema DEBE asignar una identidad propia y única dentro del sistema a cada
  proveedor registrado exitosamente. Esta especificación funcional no impone ningún formato
  concreto para dicha identidad, ya que su representación técnica corresponde a la fase de
  planificación. La identidad puede mostrarse o utilizarse por el usuario cuando sea necesario
  para referenciar inequívocamente al proveedor, pero ningún comportamiento funcional descrito en
  esta especificación DEBE depender de su formato o estructura interna.
- **FR-015**: El sistema DEBE conservar, para cada proveedor registrado, quién realizó el registro,
  la fecha y hora en que fue registrado, y el estado inicial que se le asignó.
- **FR-016**: Cuando el registro sea exitoso, el sistema DEBE confirmar al usuario que el proveedor
  quedó registrado correctamente.
- **FR-017**: Cuando el registro sea rechazado por datos obligatorios inválidos, el sistema DEBE
  informar al usuario, de forma clara, cuáles datos deben corregirse.
- **FR-018**: Cuando el registro sea rechazado por tratarse de un proveedor ya existente, el
  sistema DEBE informar al usuario que ya existe un registro previo con esa combinación de país e
  identificador fiscal, sin exponer la identidad interna, la razón social ni el estado del
  proveedor existente.
- **FR-019**: El sistema DEBE asumir que únicamente usuarios internos autorizados para gestionar
  proveedores pueden ejecutar el registro de un nuevo proveedor. La validación concreta de
  autenticación y autorización queda fuera del alcance de esta especificación.
- **FR-020**: El sistema NO DEBE exigir que la razón social sea única; distintos proveedores
  pueden compartir la misma razón social, siempre que su combinación de país e identificador
  fiscal sea diferente.

### Entidades Clave

- **Proveedor**: Representa a una organización externa que inicia un proceso de onboarding.
  Atributos relevantes para esta funcionalidad: identidad propia dentro del sistema, razón social,
  país, identificador fiscal, nombre de la persona de contacto, correo electrónico de contacto,
  estado (en el alcance de esta especificación, únicamente "Pendiente"), quién lo registró y
  fecha/hora de registro. La combinación de país e identificador fiscal identifica de manera única
  a un proveedor.

## Criterios de Éxito *(obligatorio)*

### Resultados Medibles

- **SC-001**: Un usuario autorizado puede completar el registro de un proveedor válido en un único
  intento, recibiendo confirmación inmediata del éxito.
- **SC-002**: El 100% de los intentos de registro con al menos un dato obligatorio inválido son
  rechazados y devuelven al usuario información suficiente para identificar qué corregir.
- **SC-003**: El 100% de los intentos de registro de un proveedor con una combinación de país e
  identificador fiscal ya existente son rechazados, informando al usuario sobre la existencia
  previa.
- **SC-004**: El 100% de los proveedores registrados exitosamente quedan en estado "Pendiente"
  inmediatamente después del registro, sin excepción.
- **SC-005**: El 100% de los proveedores registrados exitosamente conservan un registro verificable
  de quién y cuándo fueron creados.
- **SC-006**: Proveedores de países distintos que comparten el mismo valor textual de identificador
  fiscal pueden registrarse exitosamente como entidades independientes, sin generar falsos
  rechazos por duplicado.

## Supuestos

- Se asume que el conjunto de países válidos soportados por el sistema corresponde a la lista de
  países reconocidos internacionalmente según el estándar ISO 3166-1, excluyendo territorios
  dependientes y entidades no soberanas.
- Se asume que las diferencias de formato irrelevantes en el identificador fiscal se resuelven
  eliminando espacios adicionales, diferencias de mayúsculas/minúsculas y separadores comunes (por
  ejemplo guiones, puntos o barras), sin que esto implique validar la estructura o composición
  propia del identificador fiscal de cada país.
- Esta especificación no define longitudes máximas ni formatos adicionales para la razón social, el
  identificador fiscal o el nombre de contacto más allá de la condición de no estar vacíos.
- Se asume que la validación del correo electrónico de contacto se limita a verificar su formato;
  no se exige pertenencia a un dominio específico ni unicidad entre proveedores.
- Se asume que la autenticación y autorización de usuarios internos son provistas por una
  capacidad existente del sistema, fuera del alcance de esta especificación.
- Se asume que el registro de un proveedor es una operación transaccional individual; no se
  definen requerimientos de volumen, carga masiva o concurrencia extrema en este alcance, más allá
  de garantizar que no se generen duplicados ante intentos simultáneos.
- Se asume que la unicidad de la combinación país + identificador fiscal se mantiene sin
  excepción independientemente del estado actual o futuro que llegue a tener un proveedor
  existente. Los procesos que permitan reiniciar un onboarding, reactivar un proveedor o modificar
  su estado operarán sobre la identidad ya existente del proveedor y se definirán mediante
  especificaciones independientes; esta especificación no introduce un modelo de "Onboarding",
  historial de onboarding, nuevos estados ni decisiones de arquitectura relacionadas.
- Quedan fuera de alcance de esta especificación: carga o procesamiento de documentación,
  aprobación de proveedores, rechazo de proveedores, análisis de riesgo, capacidades de IA
  generativa, envío de correos o notificaciones, integración con sistemas externos, procesamiento
  asincrónico y cualquier decisión de arquitectura, persistencia o tecnología. Estas capacidades se
  especificarán como funcionalidades independientes cuando corresponda.
