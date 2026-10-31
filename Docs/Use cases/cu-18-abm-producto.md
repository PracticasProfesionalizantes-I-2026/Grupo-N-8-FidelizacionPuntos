# Caso de Uso: ABM Producto

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-24 (nombre de producto único), RN-25 (precio mayor a 0, hasta 999.999,99 y con 2 decimales como máximo)
> y RN-26 (producto inactivo no disponible para registrar compras)
> **implementadas** (ver sección 7); cada caso borde cuenta con su test unitario
> e integración (ver matriz de trazabilidad). RN-26 se termina de aplicar en
> CU-10, que es donde se registran las compras.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-18 |
| **Nombre** | ABM Producto |
| **Actor Principal** | Admin |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Admin → mantener el catálogo de productos actualizado; Empleado → contar con productos válidos y con un precio correcto al registrar una compra (CU-10) |
| **Disparador (Trigger)** | El admin accede a la gestión de productos |
| **Prioridad / Frecuencia** | Media; uso ocasional |
| **Reglas de negocio relacionadas** | RN-24 (nombre de producto único); RN-25 (precio mayor a 0, con hasta 6 dígitos enteros y 2 decimales); RN-26 (producto inactivo no disponible para registrar compras) |

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
   (en alta/modificación) sea estructuralmente correcto, mediante los
   DataAnnotations de los DTOs: en el alta el nombre es obligatorio; en la
   modificación es opcional, pero si se envía no puede estar en blanco.
3. La **Capa de Negocio** (`ProductoService`) verifica que el producto exista
   (en modificación o desactivación), que el precio sea mayor a 0 (**RN-25**) y
   que el nombre no pertenezca a otro producto (**RN-24**), tanto en el alta como
   en la modificación cuando el nombre cambia.
4. La **Capa de Persistencia** registra los cambios en la tabla `Productos`.
5. El Sistema devuelve un código **201 Created** (alta), **200 OK** (modificación)
   o **200 OK** (desactivación), informando el resultado de la operación.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Datos inválidos o inexistentes (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 los datos son inválidos o faltan campos obligatorios.
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request** con el detalle de los
     errores por campo (formato estándar ProblemDetails), por ejemplo: "El nombre
     es obligatorio." (alta) o "El nombre no puede estar vacío." (modificación).
     El flujo retorna al Paso 1.

* **3a. Precio inválido (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 3 (alta o modificación) el precio ingresado es menor o
     igual a 0, supera 999.999,99 (más de 6 dígitos enteros) o tiene más de 2 decimales
     (ej. 1,999), violando **RN-25**.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `PrecioInvalidoException`.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje que
     corresponda: "El precio debe ser mayor a 0", "El precio no puede superar
     999999.99" o "El precio puede tener como máximo 2 decimales". El flujo
     retorna al Paso 1.
  4. Si en la modificación no se envía precio, esta validación no se aplica.

* **3b. Nombre de producto ya existente (HTTP 409 Conflict):**
  1. El sistema detecta que en el Paso 3 (alta, o modificación con cambio de
     nombre) el nombre ingresado ya corresponde a **otro** producto existente,
     violando **RN-24**. Enviar en la modificación el mismo nombre que el producto
     ya tiene no es un conflicto.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `ProductoDuplicadoException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "El producto
     ingresado ya existe.". El flujo retorna al Paso 1.

* **3c. Producto inexistente (HTTP 404 Not Found):**
  1. El sistema detecta que en el Paso 3 (modificación o desactivación) el `id` no corresponde a
     ningún producto registrado.
  2. La Capa de Negocio no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found** con el mensaje: "El producto
     no existe.". Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Alta:** `POST /api/admin/productos` con `nombre`, `descripcion`,
   `precio` → `201 Created`.
2. **Modificación (parcial):** `PUT /api/admin/productos/{id}` con los campos a
   actualizar → `200 OK`. Solo se modifican los campos enviados; los omitidos
   conservan su valor.
3. **Ocultar / desactivar:** `PATCH /api/admin/productos/{id}/desactivar` →
   `200 OK`; a partir de ese momento, por **RN-26**, el producto deja de estar
   disponible para registrar nuevas compras (CU-10).
4. **Consultar (admin):** `GET /api/admin/productos` → `200 OK` con **todos** los
   productos, activos e inactivos, ordenados por nombre, para gestionar el catálogo.
5. **Consultar (empleado):** `GET /api/productos` (token de empleado, CU-09) →
   `200 OK` solo con los productos **activos** (**RN-26**). De acá el empleado
   obtiene el `productoId` que envía al registrar una compra (CU-10).
6. En ambas consultas, una lista vacía no es un error: `200 OK` con `items: []` y
   un `mensaje` ("No hay productos registrados" / "No hay productos disponibles").

### 6. POSTCONDICIONES
1. El producto queda creado, actualizado o desactivado en la tabla `Productos`.
   La desactivación es **lógica** (`Activo = false`): la fila no se borra, para
   no perder la referencia de las compras que ya lo usaron.
2. Un producto desactivado no puede seleccionarse para registrar nuevas
   acumulaciones de puntos (CU-10) (**RN-26**).

### 7. DETALLE DE IMPLEMENTACIÓN

| Regla / Validación | Dónde se aplica | Respuesta |
| --- | --- | --- |
| Nombre obligatorio (alta) | DTO: `[Required]` en `ProductoCreateDTO` | 400 |
| Nombre no en blanco (modificación) | DTO: `[RegularExpression(@".*\S.*")]` en `ProductoUpdateDTO` | 400 |
| Producto existente | `ProductoService`: `GetByIdAsync` | 404 |
| RN-25: 0 < precio ≤ 999.999,99, máximo 2 decimales | `ProductoService.ValidarPrecio` (alta y modificación, antes de consultar la base) | 400 |
| RN-24: nombre único | `ProductoService` + índice único en `Productos.Nombre` | 409 |
| RN-26: producto inactivo | `ProductoService`: desactivación lógica (se completa en CU-10) | 200 |

* **Validación vs. verificación:** los formatos del dato (campos obligatorios, en
  blanco, longitud) se validan en la Capa de Presentación; las reglas de negocio
  (RN-24, RN-25, RN-26) se verifican en la Capa de Negocio.
* **Autenticación:** los endpoints exigen el token del login de admin (CU-14, RN-11); cada operación queda en la auditoría (CU-21).
* **Colección Bruno:** `bruno/Admin/Productos/` cubre el alta, el precio inválido,
  el precio con muchos decimales, el precio excesivo, el duplicado, la modificación, el id inexistente, la desactivación y el listado
  del admin; el listado del empleado está en `bruno/Movimientos/Listar Productos Activos`.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al modificar o desactivar un producto existente, o al consultar el catálogo. |
| `201` | Created | Confirmación de persistencia exitosa del nuevo recurso Producto. |
| `400` | Bad Request | Datos inválidos (nombre faltante o en blanco) o precio fuera de rango o con más de 2 decimales (RN-25). |
| `404` | Not Found | Producto inexistente en modificación o desactivación. |
| `403` | Forbidden | Consulta del catálogo con el token de otro rol (ej. cliente), RN-11. |
| `409` | Conflict | Violación de RN-24: nombre de otro producto existente, en el alta o en la modificación. |

### Matriz de trazabilidad CU-18 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (alta) | `201 Created` | `CrearProductoAsync_WithValidData_CreatesAndReturnsProducto` | `AbmProducto_Create_WithValidData_Returns201Created` |
| Flujo principal (modificación) | `200 OK` | `ActualizarProductoAsync_WithValidData_UpdatesAndReturnsProducto` | `AbmProducto_Update_WithValidData_Returns200OK` |
| Flujo principal (desactivación) | `200 OK` | `DesactivarProductoAsync_WithExistingId_DeactivatesProducto` | `AbmProducto_Deactivate_WithExistingId_Returns200OK` |
| 2a. Datos inválidos (alta) | `400 Bad Request` | — (validación de esquema) | `AbmProducto_WithInvalidData_Returns400BadRequest` |
| 2a. Nombre en blanco (modificación) | `400 Bad Request` | — (validación de esquema) | `AbmProducto_Update_WithBlankNombre_Returns400BadRequest` |
| 3a. Precio inválido | `400 Bad Request` | `CrearProductoAsync_WithNonPositivePrecio_ThrowsPrecioInvalidoException` | `AbmProducto_Create_WithNonPositivePrecio_Returns400BadRequest` |
| 3a. Precio con más de 6 enteros o 2 decimales | `400 Bad Request` | `CrearProductoAsync_WithPrecioOutOfFormat_ThrowsPrecioInvalidoException`, `ActualizarProductoAsync_WithTooManyDecimals_ThrowsPrecioInvalidoException` | `AbmProducto_Create_WithTooManyDecimals_Returns400BadRequest`, `AbmProducto_Create_WithPrecioAboveMaximum_Returns400BadRequest` |
| 3a. Caso borde: precio máximo 999.999,99 | `201 Created` | `CrearProductoAsync_WithMaximumPrecio_CreatesProducto` | — |
| 3b. Nombre ya existente (alta) | `409 Conflict` | `CrearProductoAsync_WhenNombreExists_ThrowsProductoDuplicadoException` | `AbmProducto_Create_WhenNombreExists_Returns409Conflict` |
| 3b. Nombre de otro producto (modificación) | `409 Conflict` | `ActualizarProductoAsync_WhenNombreExists_ThrowsProductoDuplicadoException` | `AbmProducto_Update_WhenNombreBelongsToOther_Returns409Conflict` |
| 3c. Producto inexistente | `404 Not Found` | `ActualizarProductoAsync_WithNonExistentId_ThrowsProductoNotFoundException` | `AbmProducto_Update_WithNonExistentId_Returns404NotFound` |
| Sub-variación 4. Consulta del admin | `200 OK` | `ListarProductosAsync_ReturnsAllProductosOrderedByNombre` | `ListarProductos_AsAdmin_Returns200OKIncludingInactive` |
| Sub-variación 5. Consulta del empleado (RN-26) | `200 OK` | `ListarProductosActivosAsync_ExcludesInactiveProductos` | `ListarProductos_AsEmpleado_Returns200OKOnlyActive` |
| Sub-variación 6. Catálogo vacío | `200 OK` + mensaje | `ListarProductosActivosAsync_WhenNoProductos_ReturnsEmptyListWithMensaje` | — |
| Consulta con otro rol (RN-11) | `403 Forbidden` | — (atributo `[Authorize]`) | `ListarProductos_AsCliente_Returns403Forbidden` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
