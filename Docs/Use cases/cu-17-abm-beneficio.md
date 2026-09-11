# Caso de Uso: ABM Beneficio

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-16 (costo mayor a 0), RN-17 (beneficio inactivo no
> canjeable) y RN-23 (nombre de beneficio único) **a implementar**; cada caso
> borde debe contar con su test unitario e integración (ver matriz de
> trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-17 |
| **Nombre** | ABM Beneficio |
| **Actor Principal** | Admin |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Admin → mantener el catálogo de beneficios actualizado; Cliente → ver siempre beneficios vigentes y con un costo correcto |
| **Disparador (Trigger)** | El admin accede a la gestión de beneficios |
| **Prioridad / Frecuencia** | Media; uso ocasional |
| **Reglas de negocio relacionadas** | RN-16 (costo en puntos mayor a 0); RN-17 (beneficio inactivo no canjeable); RN-23 (nombre de beneficio único) |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador crear, modificar, ocultar o desactivar beneficios
disponibles para los clientes.

### 2. PRECONDICIONES
1. El admin se encuentra autenticado (Token JWT válido, CU-14) con permisos sobre
   el recurso Beneficios.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200/201/204)
1. El Actor envía una petición al endpoint correspondiente a la operación
   elegida: `POST /api/admin/beneficios` (alta), `PUT /api/admin/beneficios/{id}`
   (modificar) o `PATCH /api/admin/beneficios/{id}/desactivar` (desactivar).
2. La **Capa de Presentación** (`AdminBeneficiosController`) valida que el JSON
   (en alta/modificación) sea estructuralmente correcto.
3. La **Capa de Negocio** (`BeneficioAdminService`) valida que el costo en puntos
   sea mayor a 0 (**RN-16**) y que el nombre no esté ya registrado, en el alta
   (**RN-23**); y que el beneficio exista, en modificación o desactivación.
4. La **Capa de Persistencia** registra los cambios en la tabla `Beneficios`.
5. El Sistema devuelve un código **201 Created** (alta), **200 OK** (modificación)
   o **200 OK** (desactivación), informando el resultado de la operación.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Datos inválidos o inexistentes (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 los datos son inválidos o faltan campos obligatorios.
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "Los datos
     ingresados son inválidos o no existen". El flujo retorna al Paso 1.

* **3a. Costo en puntos inválido (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 3 (alta o modificación) el costo en puntos ingresado es menor o
     igual a 0, violando **RN-16**.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `CostoInvalidoException`.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "El costo
     en puntos debe ser mayor a 0". El flujo retorna al Paso 1.

* **3b. Beneficio a crear ya existe (HTTP 409 Conflict):**
  1. El sistema detecta que en el Paso 3 (alta) el nombre ingresado ya corresponde a un
     beneficio existente, violando **RN-23**.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `BeneficioDuplicadoException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "El beneficio
     ingresado ya existe". El flujo retorna al Paso 1.

* **3c. Beneficio inexistente (HTTP 404 Not Found):**
  1. El sistema detecta que en el Paso 3 (modificación o desactivación) el `id` no corresponde a
     ningún beneficio registrado.
  2. La Capa de Negocio no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Alta:** `POST /api/admin/beneficios` con `nombre`, `descripcion`,
   `costoPuntos` → `201 Created`.
2. **Modificación:** `PUT /api/admin/beneficios/{id}` con los campos a actualizar
   → `200 OK`.
3. **Ocultar / desactivar:** `PATCH /api/admin/beneficios/{id}/desactivar` →
   `200 OK`; a partir de ese momento, por **RN-17**, el beneficio deja de
   mostrarse en el catálogo (CU-06) y no puede canjearse (CU-07, CU-11).

### 6. POSTCONDICIONES
1. El beneficio queda creado, actualizado o desactivado en la tabla `Beneficios`.
2. Un beneficio desactivado no aparece en el catálogo público (**RN-07**) ni puede
   canjearse (**RN-17**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al modificar o desactivar un beneficio existente. |
| `201` | Created | Confirmación de persistencia exitosa del nuevo recurso Beneficio. |
| `400` | Bad Request | Datos inválidos o costo en puntos ≤ 0 (RN-16). |
| `404` | Not Found | Beneficio inexistente en modificación o desactivación. |
| `409` | Conflict | Violación de RN-23: nombre de beneficio ya existente en el alta. |

### Matriz de trazabilidad CU-17 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (alta) | `201 Created` | `CrearBeneficioAsync_WithValidData_CreatesAndReturnsBeneficio` | `AbmBeneficio_Create_WithValidData_Returns201Created` |
| Flujo principal (modificación) | `200 OK` | `ActualizarBeneficioAsync_WithValidData_UpdatesAndReturnsBeneficio` | `AbmBeneficio_Update_WithValidData_Returns200OK` |
| Flujo principal (desactivación) | `200 OK` | `DesactivarBeneficioAsync_WithExistingId_DeactivatesBeneficio` | `AbmBeneficio_Deactivate_WithExistingId_Returns200OK` |
| 2a. Datos inválidos | `400 Bad Request` | — (validación de esquema) | `AbmBeneficio_WithInvalidData_Returns400BadRequest` |
| 3a. Costo en puntos inválido | `400 Bad Request` | `CrearBeneficioAsync_WithNonPositiveCosto_ThrowsCostoInvalidoException` | `AbmBeneficio_Create_WithNonPositiveCosto_Returns400BadRequest` |
| 3b. Beneficio a crear ya existe | `409 Conflict` | `CrearBeneficioAsync_WhenNombreExists_ThrowsBeneficioDuplicadoException` | `AbmBeneficio_Create_WhenNombreExists_Returns409Conflict` |
| 3c. Beneficio inexistente | `404 Not Found` | `ActualizarBeneficioAsync_WithNonExistentId_ThrowsBeneficioNotFoundException` | `AbmBeneficio_Update_WithNonExistentId_Returns404NotFound` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
