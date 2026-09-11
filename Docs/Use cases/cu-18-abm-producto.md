# Caso de Uso: ABM Producto

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-24 (nombre de producto único), RN-25 (precio mayor a 0)
> y RN-26 (producto inactivo no disponible para registrar compras) **a
> implementar**; cada caso borde debe contar con su test unitario e
> integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-18 |
| **Nombre** | ABM Producto |
| **Actor Principal** | Admin |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Admin → mantener el catálogo de productos actualizado; Empleado → contar con productos válidos y con un precio correcto al registrar una compra (CU-10) |
| **Disparador (Trigger)** | El admin accede a la gestión de productos |
| **Prioridad / Frecuencia** | Media; uso ocasional |
| **Reglas de negocio relacionadas** | RN-24 (nombre de producto único); RN-25 (precio mayor a 0); RN-26 (producto inactivo no disponible para registrar compras) |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador crear, modificar, ocultar o desactivar productos
disponibles para ser registrados en las compras de los clientes.

### 2. PRECONDICIONES
1. El admin se encuentra autenticado (Token JWT válido, CU-14) con permisos sobre
   el recurso Productos.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200/201/204)
1. El Actor envía una petición al endpoint correspondiente a la operación
   elegida: `POST /api/admin/productos` (alta), `PUT /api/admin/productos/{id}`
   (modificar) o `PATCH /api/admin/productos/{id}/desactivar` (desactivar).
2. La **Capa de Presentación** (`AdminProductosController`) valida que el JSON
   (en alta/modificación) sea estructuralmente correcto.
3. La **Capa de Negocio** (`ProductoAdminService`) valida que el precio sea
   mayor a 0 (**RN-25**) y que el nombre no esté ya registrado, en el alta
   (**RN-24**); y que el producto exista, en modificación o desactivación.
4. La **Capa de Persistencia** registra los cambios en la tabla `Productos`.
5. El Sistema devuelve un código **201 Created** (alta), **200 OK** (modificación)
   o **200 OK** (desactivación), informando el resultado de la operación.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Datos inválidos o inexistentes (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 los datos son inválidos o faltan campos obligatorios.
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "Los datos
     ingresados son inválidos o no existen". El flujo retorna al Paso 1.

* **3a. Precio inválido (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 3 (alta o modificación) el precio ingresado es menor o
     igual a 0, violando **RN-25**.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `PrecioInvalidoException`.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "El
     precio debe ser mayor a 0". El flujo retorna al Paso 1.

* **3b. Producto a crear ya existe (HTTP 409 Conflict):**
  1. El sistema detecta que en el Paso 3 (alta) el nombre ingresado ya corresponde a un
     producto existente, violando **RN-24**.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `ProductoDuplicadoException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "El producto
     ingresado ya existe". El flujo retorna al Paso 1.

* **3c. Producto inexistente (HTTP 404 Not Found):**
  1. El sistema detecta que en el Paso 3 (modificación o desactivación) el `id` no corresponde a
     ningún producto registrado.
  2. La Capa de Negocio no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Alta:** `POST /api/admin/productos` con `nombre`, `descripcion`,
   `precio` → `201 Created`.
2. **Modificación:** `PUT /api/admin/productos/{id}` con los campos a actualizar
   → `200 OK`.
3. **Ocultar / desactivar:** `PATCH /api/admin/productos/{id}/desactivar` →
   `200 OK`; a partir de ese momento, por **RN-26**, el producto deja de estar
   disponible para registrar nuevas compras (CU-10).

### 6. POSTCONDICIONES
1. El producto queda creado, actualizado o desactivado en la tabla `Productos`.
2. Un producto desactivado no puede seleccionarse para registrar nuevas
   acumulaciones de puntos (CU-10) (**RN-26**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al modificar o desactivar un producto existente. |
| `201` | Created | Confirmación de persistencia exitosa del nuevo recurso Producto. |
| `400` | Bad Request | Datos inválidos o precio ≤ 0 (RN-25). |
| `404` | Not Found | Producto inexistente en modificación o desactivación. |
| `409` | Conflict | Violación de RN-24: nombre de producto ya existente en el alta. |

### Matriz de trazabilidad CU-18 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (alta) | `201 Created` | `CrearProductoAsync_WithValidData_CreatesAndReturnsProducto` | `AbmProducto_Create_WithValidData_Returns201Created` |
| Flujo principal (modificación) | `200 OK` | `ActualizarProductoAsync_WithValidData_UpdatesAndReturnsProducto` | `AbmProducto_Update_WithValidData_Returns200OK` |
| Flujo principal (desactivación) | `200 OK` | `DesactivarProductoAsync_WithExistingId_DeactivatesProducto` | `AbmProducto_Deactivate_WithExistingId_Returns200OK` |
| 2a. Datos inválidos | `400 Bad Request` | — (validación de esquema) | `AbmProducto_WithInvalidData_Returns400BadRequest` |
| 3a. Precio inválido | `400 Bad Request` | `CrearProductoAsync_WithNonPositivePrecio_ThrowsPrecioInvalidoException` | `AbmProducto_Create_WithNonPositivePrecio_Returns400BadRequest` |
| 3b. Producto a crear ya existe | `409 Conflict` | `CrearProductoAsync_WhenNombreExists_ThrowsProductoDuplicadoException` | `AbmProducto_Create_WhenNombreExists_Returns409Conflict` |
| 3c. Producto inexistente | `404 Not Found` | `ActualizarProductoAsync_WithNonExistentId_ThrowsProductoNotFoundException` | `AbmProducto_Update_WithNonExistentId_Returns404NotFound` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
