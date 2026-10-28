# Caso de Uso: Configurar Reglas de Acumulación

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-18 (versión activa única) y RN-13 (trazabilidad para
> auditoría) **implementadas** (ver sección 7); cada caso borde cuenta con su test
> unitario e integración (ver matriz de trazabilidad).
> RF-22 ("el admin aplica lapso de vencimiento a los puntos") no tenía un caso
> de uso propio en la documentación original; se decidió incorporar ese lapso
> (`diasVigenciaPuntos`) como un campo más de esta misma regla en vez de crear
> un CU y una pantalla de configuración separados, ya que ambos valores
> (puntos por monto y días de vigencia) se definen juntos y con la misma
> frecuencia de uso.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-19 |
| **Nombre** | Configurar reglas de acumulación |
| **Actor Principal** | Admin |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Admin → ajustar cómo se generan los puntos según la estrategia comercial; Empleado → operar acumulaciones (CU-10) con reglas siempre vigentes y sin ambigüedad; Negocio → que el criterio de acumulación quede documentado y auditable |
| **Disparador (Trigger)** | El admin decide crear o modificar una regla de acumulación |
| **Prioridad / Frecuencia** | Baja; uso ocasional |
| **Reglas de negocio relacionadas** | RN-18 (una única versión activa por regla); RN-13 (registro para auditoría); RN-21/RF-22 (lapso de vencimiento de los puntos acreditados bajo la regla) |

---

### 1. BREVE DESCRIPCIÓN
Permite al admin consultar, definir o modificar los criterios utilizados para
calcular la cantidad de puntos que recibe el cliente por sus compras.

### 2. PRECONDICIONES
1. El admin se encuentra autenticado (Token JWT válido, CU-14) con permisos sobre
   el recurso Reglas de Acumulación.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200/201)
1. El Actor envía una petición al endpoint correspondiente a la operación
   elegida: `GET /api/admin/reglas-acumulacion` (listar todas), `GET
   /api/admin/reglas-acumulacion/{id}` (detalle), `POST
   /api/admin/reglas-acumulacion` (crear) o `PUT
   /api/admin/reglas-acumulacion/{id}` (modificar). Las operaciones de creación y
   modificación reciben un JSON con el criterio (`puntosPorMonto`,
   `vigenciaDesde`, `diasVigenciaPuntos` — RF-22: cuántos días tarda en vencer
   un lote acreditado bajo esta regla, RN-21). `puntosPorMonto` son los puntos
   otorgados por cada $1 de compra (ej.: `0.1` = 1 punto cada $10).
2. La **Capa de Presentación** (`AdminReglasAcumulacionController`) valida que el
   JSON sea estructuralmente correcto y, en la creación, que estén los tres campos
   obligatorios (DataAnnotations del DTO).
3. La **Capa de Negocio** (`ReglaAcumulacionService`) recupera la(s) regla(s)
   solicitada(s) (en consulta) o, en creación/modificación, valida que los valores
   sean mayores a 0 y verifica que ninguna otra versión empiece en la misma fecha
   (**RN-18**). La modificación es parcial: solo cambian los campos enviados.
4. La **Capa de Persistencia** consulta la tabla `ReglasAcumulacion` con
   `AsNoTracking()` (en lecturas), o guarda la regla y registra el cambio para
   auditoría (**RN-13**, en creación/modificación): `FechaCreacion` en el alta y
   `FechaModificacion` en cada modificación.
5. El Sistema devuelve un código **200 OK** (consulta o modificación) o **201
   Created** (alta) con la(s) regla(s) o la confirmación de la operación.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Configuración inválida (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 3 hay una inconsistencia en la configuración:
     `puntosPorMonto` o `diasVigenciaPuntos` menores o iguales a 0, o una
     `vigenciaDesde` igual a la de otra versión ya registrada (superposición,
     violando **RN-18**).
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `ReglaAcumulacionInvalidaException`.
  3. El Sistema devuelve un código **400 Bad Request** con el motivo, por ejemplo:
     "Ya existe una regla que entra en vigencia en esa fecha.". El flujo retorna
     al Paso 1.

* **3b. Falta de datos (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 (creación) faltan datos obligatorios de la regla.
  2. El Sistema (Capa de Presentación, DataAnnotations) detecta los campos sin completar.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje "Campos sin
     rellenar: <campo> es obligatorio." por cada campo faltante. El flujo retorna
     al Paso 1.

* **3c. Regla inexistente (HTTP 404 Not Found):**
  1. El sistema detecta que en el Paso 3 el `id` solicitado en `GET` o `PUT
     /api/admin/reglas-acumulacion/{id}` no corresponde a ninguna regla
     registrada.
  2. La Capa de Negocio no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found** con el mensaje: "La regla de
     acumulación no existe.". Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: la configuración se gestiona por un único canal (panel de
administración web)._

### 6. POSTCONDICIONES
1. En consulta: no hay cambios de estado; el Actor recibe la(s) regla(s)
   solicitada(s).
2. En creación/modificación: la regla queda almacenada en la tabla
   `ReglasAcumulacion`.
3. La nueva regla queda disponible para ser aplicada en futuras acumulaciones
   (CU-10) y su creación/modificación queda registrada para auditoría (**RN-13**).

### 7. DETALLE DE IMPLEMENTACIÓN

* **Versiones (RN-18):** la regla no se pisa: cada alta crea una **versión nueva**
  con su propia `vigenciaDesde` (se guarda solo la fecha, sin hora). La versión
  vigente en un día dado es la de `vigenciaDesde` más reciente que ya empezó; así
  se puede programar un cambio a futuro sin borrar la regla anterior. Dos versiones
  con la misma fecha de inicio serían dos reglas vigentes el mismo día, por eso se
  rechazan (400), con un índice único en `VigenciaDesde` como respaldo en la base.
* **Auditoría (RN-13):** el historial de versiones más `FechaCreacion` y
  `FechaModificacion` deja registrado qué regla regía en cada momento. Registrar
  **qué admin** hizo el cambio queda para una versión futura, cuando exista el
  login de admin (CU-14).
* **Datos iniciales:** el seed carga una regla vigente desde hace 30 días
  (`0.1` puntos por $1 y 180 días de vigencia), que es la que usa CU-10.
* **Colección Bruno:** `bruno/Admin/Reglas Acumulacion/` (alta con fecha al azar,
  superposición, datos faltantes, listado, detalle, id inexistente y modificación).

| Regla / Validación | Dónde se aplica | Respuesta |
| --- | --- | --- |
| Campos obligatorios (alta) | DTO: `[Required]` sobre tipos nullable | 400 |
| Valores mayores a 0 | `ReglaAcumulacionService` (antes de consultar la base) | 400 |
| RN-18: sin superposición de fechas | `ReglaAcumulacionService` + índice único | 400 |
| Regla existente | `ReglaAcumulacionService`: `GetByIdAsync` | 404 |
| RN-13: fechas de auditoría | `ReglaAcumulacionService` | — |

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al listar, consultar el detalle o modificar una regla existente. |
| `201` | Created | Confirmación de persistencia exitosa de la nueva regla. |
| `400` | Bad Request | Configuración inválida (valores ≤ 0 o superposición, RN-18) o campos obligatorios faltantes. |
| `404` | Not Found | El `id` de la regla consultada o modificada no existe. |

### Matriz de trazabilidad CU-19 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (listar) | `200 OK` | `ObtenerReglasAcumulacionAsync_ReturnsAllRules` | `ConsultarReglasAcumulacion_Returns200OK` |
| Flujo principal (detalle) | `200 OK` | `ObtenerReglaAcumulacionPorIdAsync_WithExistingId_ReturnsRule` | `ConsultarReglaAcumulacionPorId_WithExistingId_Returns200OK` |
| Flujo principal (alta) | `201 Created` | `CrearReglaAcumulacionAsync_WithValidData_CreatesAndLogsAudit` | `ConfigurarReglaAcumulacion_Create_WithValidData_Returns201Created` |
| Flujo principal (modificación) | `200 OK` | `ActualizarReglaAcumulacionAsync_WithValidData_UpdatesAndLogsAudit` | `ConfigurarReglaAcumulacion_Update_WithValidData_Returns200OK` |
| 3a. Configuración inválida (superposición) | `400 Bad Request` | `CrearReglaAcumulacionAsync_WhenOverlappingActiveRule_ThrowsReglaAcumulacionInvalidaException` | `ConfigurarReglaAcumulacion_WithOverlappingActiveRule_Returns400BadRequest` |
| 3a. Configuración inválida (valores ≤ 0) | `400 Bad Request` | `CrearReglaAcumulacionAsync_WithNonPositiveValues_ThrowsReglaAcumulacionInvalidaException` | — (cubierto por el unitario) |
| 3a. Superposición en la modificación | `400 Bad Request` | `ActualizarReglaAcumulacionAsync_WhenOverlappingOtherRule_ThrowsReglaAcumulacionInvalidaException` | — (cubierto por el unitario) |
| 3b. Falta de datos | `400 Bad Request` | — (validación de esquema) | `ConfigurarReglaAcumulacion_WithMissingFields_Returns400BadRequest` |
| 3c. Regla inexistente (detalle) | `404 Not Found` | `ObtenerReglaAcumulacionPorIdAsync_WithUnknownId_ThrowsReglaAcumulacionNotFoundException` | `ConsultarReglaAcumulacionPorId_WithUnknownId_Returns404NotFound` |
| 3c. Regla inexistente (modificación) | `404 Not Found` | `ActualizarReglaAcumulacionAsync_WithUnknownId_ThrowsReglaAcumulacionNotFoundException` | — (cubierto por el unitario) |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
