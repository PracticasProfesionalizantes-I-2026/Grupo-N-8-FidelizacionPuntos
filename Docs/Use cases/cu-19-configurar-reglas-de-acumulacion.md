# Caso de Uso: Configurar Reglas de Acumulación

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-18 (versión activa única) y RN-13 (trazabilidad para
> auditoría) **a implementar**; cada caso borde debe contar con su test unitario e
> integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-19 |
| **Nombre** | Configurar reglas de acumulación |
| **Actor Principal** | Admin |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Admin → ajustar cómo se generan los puntos según la estrategia comercial; Empleado → operar acumulaciones (CU-10) con reglas siempre vigentes y sin ambigüedad; Negocio → que el criterio de acumulación quede documentado y auditable |
| **Disparador (Trigger)** | El admin decide crear o modificar una regla de acumulación |
| **Prioridad / Frecuencia** | Baja; uso ocasional |
| **Reglas de negocio relacionadas** | RN-18 (una única versión activa por regla); RN-13 (registro para auditoría) |

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
   `vigenciaDesde`).
2. La **Capa de Presentación** (`AdminReglasAcumulacionController`) valida que el
   JSON sea estructuralmente correcto (en creación/modificación).
3. La **Capa de Negocio** (`ReglaAcumulacionService`) recupera la(s) regla(s)
   solicitada(s) (en consulta) o, en creación/modificación, valida la
   configuración y verifica que no exista ya otra versión activa de la misma
   regla (**RN-18**).
4. La **Capa de Persistencia** consulta la tabla `ReglasAcumulacion` con
   `AsNoTracking()` (en lecturas), o guarda la regla y registra el cambio para
   auditoría (**RN-13**, en creación/modificación).
5. El Sistema devuelve un código **200 OK** (consulta o modificación) o **201
   Created** (alta) con la(s) regla(s) o la confirmación de la operación.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Configuración inválida (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 3 el Sistema detecta una inconsistencia en la configuración
     (ej. valores negativos o rangos de vigencia superpuestos con una regla ya
     activa, violando **RN-18**).
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `ReglaAcumulacionInvalidaException`.
  3. El Sistema informa el error. El flujo retorna al Paso 1.

* **3b. Falta de datos (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 3 faltan datos obligatorios de la regla.
  2. El Sistema (Capa de Negocio) detecta los campos sin completar.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "Campos sin
     rellenar". El flujo retorna al Paso 1.

* **3c. Regla inexistente al consultar el detalle (HTTP 404 Not Found):**
  1. El sistema detecta que en el Paso 3 el `id` solicitado en `GET
     /api/admin/reglas-acumulacion/{id}` no corresponde a ninguna regla
     registrada.
  2. La Capa de Negocio no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

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

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al listar, consultar el detalle o modificar una regla existente. |
| `201` | Created | Confirmación de persistencia exitosa de la nueva regla. |
| `400` | Bad Request | Configuración inválida (RN-18) o campos obligatorios faltantes. |
| `404` | Not Found | El `id` de la regla consultada no existe. |

### Matriz de trazabilidad CU-19 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (listar) | `200 OK` | `ObtenerReglasAcumulacionAsync_ReturnsAllRules` | `ConsultarReglasAcumulacion_Returns200OK` |
| Flujo principal (detalle) | `200 OK` | `ObtenerReglaAcumulacionPorIdAsync_WithExistingId_ReturnsRule` | `ConsultarReglaAcumulacionPorId_WithExistingId_Returns200OK` |
| Flujo principal (alta) | `201 Created` | `CrearReglaAcumulacionAsync_WithValidData_CreatesAndLogsAudit` | `ConfigurarReglaAcumulacion_Create_WithValidData_Returns201Created` |
| Flujo principal (modificación) | `200 OK` | `ActualizarReglaAcumulacionAsync_WithValidData_UpdatesAndLogsAudit` | `ConfigurarReglaAcumulacion_Update_WithValidData_Returns200OK` |
| 3a. Configuración inválida | `400 Bad Request` | `CrearReglaAcumulacionAsync_WhenOverlappingActiveRule_ThrowsReglaAcumulacionInvalidaException` | `ConfigurarReglaAcumulacion_WithOverlappingActiveRule_Returns400BadRequest` |
| 3b. Falta de datos | `400 Bad Request` | — (validación de esquema) | `ConfigurarReglaAcumulacion_WithMissingFields_Returns400BadRequest` |
| 3c. Regla inexistente (detalle) | `404 Not Found` | `ObtenerReglaAcumulacionPorIdAsync_WithUnknownId_ThrowsReglaAcumulacionNotFoundException` | `ConsultarReglaAcumulacionPorId_WithUnknownId_Returns404NotFound` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
