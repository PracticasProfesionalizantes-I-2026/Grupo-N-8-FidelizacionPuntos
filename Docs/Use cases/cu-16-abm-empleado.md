# Caso de Uso: ABM Empleado

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-01 (documento/email único) y RN-15 (empleado inactivo sin
> acceso) **a implementar**; cada caso borde debe contar con su test unitario e
> integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-16 |
| **Nombre** | ABM Empleado |
| **Actor Principal** | Admin |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Admin → mantener actualizada la nómina de empleados con acceso al sistema; Empleado → conservar (o perder, si corresponde) su acceso operativo de forma correcta |
| **Disparador (Trigger)** | El admin accede a la gestión de empleados |
| **Prioridad / Frecuencia** | Baja; uso ocasional (altas/bajas de personal) |
| **Reglas de negocio relacionadas** | RN-01 (documento/email único); RN-15 (empleado dado de baja pierde el acceso) |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador dar de alta, modificar o dar de baja empleados desde el
panel de administración.

### 2. PRECONDICIONES
1. El admin se encuentra autenticado (Token JWT válido, CU-14) con permisos sobre
   el recurso Empleados.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200/201/204)
1. El Actor envía una petición al endpoint correspondiente a la operación elegida:
   `POST /api/admin/empleados` (alta), `PUT /api/admin/empleados/{id}`
   (modificar) o `DELETE /api/admin/empleados/{id}` (baja lógica).
2. La **Capa de Presentación** (`AdminEmpleadosController`) valida que el JSON (en
   alta/modificación) sea estructuralmente correcto.
3. La **Capa de Negocio** (`EmpleadoAdminService`) valida los datos según la
   operación: unicidad de documento/email en el alta (**RN-01**), existencia del
   empleado en modificación/baja.
4. La **Capa de Persistencia** registra los cambios en la tabla `Empleados`
   (creación, actualización de campos o marca de `Activo = false` en la baja).
5. El Sistema devuelve un código **201 Created** (alta), **200 OK** (modificación)
   o **204 No Content** (baja), informando el resultado de la operación.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Datos inválidos o inexistentes (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 los datos son inválidos o faltan campos obligatorios.
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "Los datos
     ingresados son inválidos o no existen". El flujo retorna al Paso 1.

* **3a. Empleado a crear ya existe (HTTP 409 Conflict):**
  1. El sistema detecta que en el Paso 3 (alta) el documento ingresado ya existe, violando **RN-01**.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `EmpleadoDuplicadoException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "El documento
     ingresado pertenece a un empleado activo". El flujo retorna al Paso 1.

* **3b. Empleado inexistente (HTTP 404 Not Found):**
  1. El sistema detecta que en el Paso 3 (modificación o baja) el `id` no corresponde a ningún
     empleado registrado.
  2. La Capa de Negocio no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Alta:** `POST /api/admin/empleados` con `nombre`, `documento`, `email` →
   `201 Created`.
2. **Modificación:** `PUT /api/admin/empleados/{id}` con los campos a actualizar →
   `200 OK`.
3. **Baja lógica:** `DELETE /api/admin/empleados/{id}` → `204 No Content`; el
   empleado queda con `Activo = false` y, por **RN-15**, pierde el acceso al
   sistema (cualquier token vigente deja de ser válido en el próximo login).

### 6. POSTCONDICIONES
1. El empleado queda creado, actualizado o dado de baja en la tabla `Empleados`
   según la operación realizada.
2. Un empleado dado de baja no puede volver a iniciar sesión (**RN-15**, CU-09).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al modificar un empleado existente. |
| `201` | Created | Confirmación de persistencia exitosa del nuevo recurso Empleado. |
| `204` | No Content | Éxito en la baja lógica del empleado (sin cuerpo en la respuesta). |
| `400` | Bad Request | Datos inválidos o incompletos. |
| `404` | Not Found | Empleado inexistente en modificación o baja. |
| `409` | Conflict | Violación de RN-01: documento ya existente en el alta. |

### Matriz de trazabilidad CU-16 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (alta) | `201 Created` | `CrearEmpleadoAsync_WithValidData_CreatesAndReturnsEmpleado` | `AbmEmpleado_Create_WithValidData_Returns201Created` |
| Flujo principal (modificación) | `200 OK` | `ActualizarEmpleadoAsync_WithValidData_UpdatesAndReturnsEmpleado` | `AbmEmpleado_Update_WithValidData_Returns200OK` |
| Flujo principal (baja) | `204 No Content` | `BajaEmpleadoAsync_WithExistingId_DeactivatesEmpleado` | `AbmEmpleado_Delete_WithExistingId_Returns204NoContent` |
| 2a. Datos inválidos | `400 Bad Request` | — (validación de esquema) | `AbmEmpleado_WithInvalidData_Returns400BadRequest` |
| 3a. Empleado a crear ya existe | `409 Conflict` | `CrearEmpleadoAsync_WhenDocumentoExists_ThrowsEmpleadoDuplicadoException` | `AbmEmpleado_Create_WhenDocumentoExists_Returns409Conflict` |
| 3b. Empleado inexistente | `404 Not Found` | `ActualizarEmpleadoAsync_WithNonExistentId_ThrowsEmpleadoNotFoundException` | `AbmEmpleado_Update_WithNonExistentId_Returns404NotFound` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
