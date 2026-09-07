# Caso de Uso: Configurar Reglas de Acumulación

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-18 (versión activa única) y RN-13 (trazabilidad para
> auditoría) **a implementar**; cada caso borde debe contar con su test unitario e
> integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-17 |
| **Nombre** | Configurar reglas de acumulación |
| **Actor Principal** | Admin |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Admin → ajustar cómo se generan los puntos según la estrategia comercial; Empleado → operar acumulaciones (CU-10) con reglas siempre vigentes y sin ambigüedad; Negocio → que el criterio de acumulación quede documentado y auditable |
| **Disparador (Trigger)** | El admin decide crear o modificar una regla de acumulación |
| **Prioridad / Frecuencia** | Baja; uso ocasional |
| **Reglas de negocio relacionadas** | RN-18 (una única versión activa por regla); RN-13 (registro para auditoría) |

---

### 1. BREVE DESCRIPCIÓN
Permite al admin definir los criterios utilizados para calcular la cantidad de
puntos que recibe el cliente por sus compras.

### 2. PRECONDICIONES
1. El admin se encuentra autenticado (Token JWT válido, CU-13) con permisos sobre
   el recurso Reglas de Acumulación.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200/201)
1. El Actor envía una petición al endpoint `POST /api/admin/reglas-acumulacion`
   (crear) o `PUT /api/admin/reglas-acumulacion/{id}` (modificar) con un JSON que
   contiene el criterio (`puntosPorMonto`, `vigenciaDesde`).
2. La **Capa de Presentación** (`AdminReglasAcumulacionController`) valida que el
   JSON sea estructuralmente correcto.
3. La **Capa de Negocio** (`ReglaAcumulacionService`) valida la configuración y
   verifica que no exista ya otra versión activa de la misma regla (**RN-18**).
4. La **Capa de Persistencia** guarda la regla en la tabla `ReglasAcumulacion` y
   registra el cambio para auditoría (**RN-13**).
5. El Sistema devuelve un código **201 Created** (alta) o **200 OK**
   (modificación) confirmando que la regla quedó guardada y disponible para
   futuras acumulaciones.

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

### 5. SUB-VARIACIONES (opcional)
_No aplica: la configuración se gestiona por un único canal (panel de
administración web)._

### 6. POSTCONDICIONES
1. La regla queda almacenada en la tabla `ReglasAcumulacion`.
2. La nueva regla queda disponible para ser aplicada en futuras acumulaciones
   (CU-10) y su creación/modificación queda registrada para auditoría (**RN-13**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al modificar una regla existente. |
| `201` | Created | Confirmación de persistencia exitosa de la nueva regla. |
| `400` | Bad Request | Configuración inválida (RN-18) o campos obligatorios faltantes. |

### Matriz de trazabilidad CU-17 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (alta) | `201 Created` | `CrearReglaAcumulacionAsync_WithValidData_CreatesAndLogsAudit` | `ConfigurarReglaAcumulacion_Create_WithValidData_Returns201Created` |
| Flujo principal (modificación) | `200 OK` | `ActualizarReglaAcumulacionAsync_WithValidData_UpdatesAndLogsAudit` | `ConfigurarReglaAcumulacion_Update_WithValidData_Returns200OK` |
| 3a. Configuración inválida | `400 Bad Request` | `CrearReglaAcumulacionAsync_WhenOverlappingActiveRule_ThrowsReglaAcumulacionInvalidaException` | `ConfigurarReglaAcumulacion_WithOverlappingActiveRule_Returns400BadRequest` |
| 3b. Falta de datos | `400 Bad Request` | — (validación de esquema) | `ConfigurarReglaAcumulacion_WithMissingFields_Returns400BadRequest` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
