# Caso de Uso: ABM Cliente

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-01 (documento/email único) y RN-14 (cliente inactivo sin
> acumulación/canje) **a implementar**; cada caso borde debe contar con su test
> unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-14 |
| **Nombre** | ABM Cliente |
| **Actor Principal** | Admin |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Admin → mantener la base de clientes correcta y actualizada; Cliente → que sus datos y su estado (activo/inactivo) en el sistema sean confiables |
| **Disparador (Trigger)** | El admin accede a la gestión de clientes |
| **Prioridad / Frecuencia** | Media; uso ocasional |
| **Reglas de negocio relacionadas** | RN-01 (documento/email único); RN-14 (cliente dado de baja no acumula ni canjea) |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador dar de alta, modificar o dar de baja clientes desde el
panel de administración.

### 2. PRECONDICIONES
1. El admin se encuentra autenticado (Token JWT válido, CU-13) con permisos sobre
   el recurso Clientes.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200/201/204)
1. El Actor envía una petición al endpoint correspondiente a la operación elegida:
   `POST /api/admin/clientes` (alta), `PUT /api/admin/clientes/{id}` (modificar) o
   `DELETE /api/admin/clientes/{id}` (baja lógica).
2. La **Capa de Presentación** (`AdminClientesController`) valida que el JSON (en
   alta/modificación) sea estructuralmente correcto.
3. La **Capa de Negocio** (`ClienteAdminService`) valida los datos según la
   operación: unicidad de documento/email en el alta (**RN-01**), existencia del
   cliente en modificación/baja.
4. La **Capa de Persistencia** registra los cambios en la tabla `Clientes`
   (creación, actualización de campos o marca de `Activo = false` en la baja).
5. El Sistema devuelve un código **201 Created** (alta), **200 OK** (modificación)
   o **204 No Content** (baja), informando el resultado de la operación.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Datos inválidos o inexistentes (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 los datos son inválidos o faltan campos obligatorios.
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "Los datos
     ingresados son inválidos o no existen". El flujo retorna al Paso 1.

* **3a. Cliente a crear ya existe (HTTP 409 Conflict):**
  1. El sistema detecta que en el Paso 3 (alta) el documento ingresado ya existe, violando **RN-01**.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `ClienteDuplicadoException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "El documento
     ingresado pertenece a un cliente activo". El flujo retorna al Paso 1.

* **3b. Cliente inexistente (HTTP 404 Not Found):**
  1. El sistema detecta que en el Paso 3 (modificación o baja) el `id` no corresponde a ningún cliente
     registrado.
  2. La Capa de Negocio no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Alta:** `POST /api/admin/clientes` con `nombre`, `documento`, `email` →
   `201 Created`.
2. **Modificación:** `PUT /api/admin/clientes/{id}` con los campos a actualizar →
   `200 OK`.
3. **Baja lógica:** `DELETE /api/admin/clientes/{id}` → `204 No Content`; el
   cliente queda con `Activo = false` y, por **RN-14**, no puede acumular ni
   canjear puntos desde ese momento.

### 6. POSTCONDICIONES
1. El cliente queda creado, actualizado o dado de baja en la tabla `Clientes`
   según la operación realizada.
2. Un cliente dado de baja queda inhabilitado para acumular (CU-10) y canjear
   (CU-07, CU-11) puntos (**RN-14**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al modificar un cliente existente. |
| `201` | Created | Confirmación de persistencia exitosa del nuevo recurso Cliente. |
| `204` | No Content | Éxito en la baja lógica del cliente (sin cuerpo en la respuesta). |
| `400` | Bad Request | Datos inválidos o incompletos. |
| `404` | Not Found | Cliente inexistente en modificación o baja. |
| `409` | Conflict | Violación de RN-01: documento ya existente en el alta. |

### Matriz de trazabilidad CU-14 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (alta) | `201 Created` | `CrearClienteAsync_WithValidData_CreatesAndReturnsCliente` | `AbmCliente_Create_WithValidData_Returns201Created` |
| Flujo principal (modificación) | `200 OK` | `ActualizarClienteAsync_WithValidData_UpdatesAndReturnsCliente` | `AbmCliente_Update_WithValidData_Returns200OK` |
| Flujo principal (baja) | `204 No Content` | `BajaClienteAsync_WithExistingId_DeactivatesCliente` | `AbmCliente_Delete_WithExistingId_Returns204NoContent` |
| 2a. Datos inválidos | `400 Bad Request` | — (validación de esquema) | `AbmCliente_WithInvalidData_Returns400BadRequest` |
| 3a. Cliente a crear ya existe | `409 Conflict` | `CrearClienteAsync_WhenDocumentoExists_ThrowsClienteDuplicadoException` | `AbmCliente_Create_WhenDocumentoExists_Returns409Conflict` |
| 3b. Cliente inexistente | `404 Not Found` | `ActualizarClienteAsync_WithNonExistentId_ThrowsClienteNotFoundException` | `AbmCliente_Update_WithNonExistentId_Returns404NotFound` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
