# Caso de Uso: Gestionar Acumulación de Puntos

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-12 (cálculo según reglas vigentes) y RN-13 (trazabilidad
> para auditoría) **implementadas**, junto con los niveles de complejidad **N1**
> (Gold x1.5) y **N2** (puntos pendientes 15 días) — ver sección 7; cada caso
> borde cuenta con su test unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-10 |
| **Nombre** | Gestionar acumulación de puntos |
| **Actor Principal** | Empleado |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Empleado → registrar la venta y acreditar puntos correctamente; Cliente → recibir los puntos que le corresponden por su compra; Negocio → aplicar de forma consistente las reglas de acumulación vigentes |
| **Disparador (Trigger)** | El empleado registra una compra realizada por un cliente |
| **Prioridad / Frecuencia** | Alta; uso muy frecuente (cada venta) |
| **Reglas de negocio relacionadas** | RN-12 (cálculo según reglas vigentes); RN-13 (registro para auditoría); RN-26 (producto inactivo); N1 (Gold x1.5); N2 (puntos pendientes 15 días) |

---

### 1. BREVE DESCRIPCIÓN
Permite al empleado registrar, en el punto de venta, una compra realizada por un
cliente y acreditarle los puntos correspondientes según las reglas de
acumulación configuradas (CU-19).

### 2. PRECONDICIONES
1. El empleado se encuentra autenticado (Token JWT válido, CU-09).
2. El cliente se encuentra registrado en el sistema.
3. Existen reglas de acumulación activas (CU-19).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El cliente realiza una compra en el local; el empleado le solicita el
   documento y lo busca (`GET /api/clientes?documento={dni}`), que devuelve el
   **id** del cliente con su nombre y categoría para confirmar que es la persona
   correcta. Luego carga los productos de la venta y, al finalizarla, el Actor
   envía una petición al endpoint `POST /api/movimientos/acumulaciones` con un
   JSON que contiene el id del cliente y el detalle de la compra
   (`clienteId`, `items: [{productoId, cantidad}]`). El monto **no** lo
   envía el empleado: el sistema lo calcula con el precio de cada producto (CU-18),
   para que no se puedan acreditar puntos de más.
2. La **Capa de Presentación** (`MovimientosController.RegistrarAcumulacion`)
   valida que el JSON sea estructuralmente correcto.
3. La **Capa de Negocio** (`MovimientoService.RegistrarAcumulacionAsync`) localiza
   al cliente por su id, calcula el monto de la compra con los
   precios de los productos activos (**RN-26**) y los puntos a acreditar con la regla
   de acumulación vigente (**RN-12**): `puntos = ⌊monto × puntosPorMonto × multiplicador⌋`,
   donde el multiplicador es 1.5 para clientes **Gold** (**N1**) y 1 para el resto.
4. La **Capa de Persistencia** registra el movimiento de acumulación
   (`Movimientos`) para fines de auditoría (**RN-13**). El lote queda **pendiente
   15 días** (**N2**, `FechaDisponible`) y vence `diasVigenciaPuntos` días después de
   estar disponible (RN-21).
5. El Sistema devuelve un código **201 Created** con la confirmación de la
   operación: monto, regla y multiplicador aplicados, puntos acreditados, fecha en
   que quedan disponibles, vencimiento y saldo disponible (que todavía no incluye
   los puntos pendientes).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Dato obligatorio faltante o producto/cantidad inválido (HTTP 400 Bad
  Request):**
  1. El sistema detecta que en el Paso 2 el empleado ingresa un producto o cantidad incorrectos; el
     empleado puede corregir/quitar el ítem mal cargado antes de reenviar la
     petición sin que el caso de uso finalice.
  2. El Sistema rechaza la petición indicando qué ítem está mal, para que el
     empleado lo corrija y reenvíe la venta. Una cantidad fuera de rango (1 a 1000) o
     un campo faltante lo detecta la Capa de Presentación (DataAnnotations); un
     producto inexistente o inactivo (**RN-26**), la Capa de Negocio
     (`ItemCompraInvalidoException`).
  3. El Sistema devuelve un código **400 Bad Request** detallando el ítem
     rechazado (ej.: "Ítem 1: el producto no existe."). El flujo retorna al Paso 1.

* **1a. DNI no registrado en la búsqueda (HTTP 404 Not Found):**
  1. El sistema detecta que en el Paso 1 el documento buscado no corresponde a ningún
     cliente registrado.
  2. El Sistema devuelve un código **404 Not Found** con el mensaje: "El documento
     ingresado no se encuentra registrado". El empleado puede darlo de alta (CU-12).

* **1b. DNI mal ingresado en la búsqueda (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 1 el documento no respeta el formato válido
     (solo números, de 7 a 10 dígitos).
  2. El Sistema (Capa de Negocio, `ClienteService.BuscarPorDocumentoAsync`) lanza
     `DocumentoInvalidoException` antes de consultar la base (fail fast).
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "El
     documento ingresado fue incorrecto". El flujo retorna al Paso 1.

* **3a. Cliente no registrado (HTTP 404 Not Found):**
  1. El sistema detecta que en el Paso 3 el `clienteId` enviado no corresponde a ningún
     cliente registrado.
  2. La Capa de Negocio no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found** con el mensaje: "El cliente
     no se encuentra registrado.". Fin del caso de uso.

> El antiguo flujo 3b (documento mal ingresado en la compra) pasó a ser el 1b: la
> compra ya no recibe el DNI, sino el id que devuelve la búsqueda. Un `clienteId`
> faltante o que no es un Guid lo rechaza la Capa de Presentación (flujo 2a, 400).

* **3d. Cliente inactivo o sin regla vigente (HTTP 409 Conflict):**
  1. El sistema detecta que en el Paso 3 el cliente está dado de baja (RN-14) o que
     no hay ninguna regla de acumulación vigente (precondición 3, CU-19).
  2. El Sistema (Capa de Negocio) lanza `CuentaInactivaException` o
     `ReglaAcumulacionNoVigenteException`.
  3. El Sistema devuelve un código **409 Conflict** con el motivo y no acredita
     puntos. Fin del caso de uso.

* **3c. El empleado detecta un cálculo de puntos incorrecto antes de
  confirmar la venta (Resultado: Sin cambios; el caso de uso no finaliza):**
  1. El sistema, en el Paso 3, calcula y exhibe al empleado el detalle de la
     compra y los puntos a acreditar antes de que confirme la venta.
  2. El empleado revisa el detalle y detecta que un producto o una cantidad
     fueron mal cargados, generando un cálculo que no corresponde a la compra
     real.
  3. El empleado corrige el carrito y solicita un nuevo cálculo; el caso de uso
     continúa desde el Paso 1 sin que la Capa de Persistencia registre ningún
     movimiento hasta que el empleado confirme la venta con el cálculo
     correcto.

* **4a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. El sistema detecta que en el Paso 4 la Capa de Persistencia no puede registrar el movimiento o
     actualizar el saldo.
  2. El Sistema (Capa de Negocio) traduce la falla a `PersistenceException`; como el
     movimiento se guarda en un único `SaveChanges`, no queda nada a medias.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de
     uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: la acumulación se registra por un único canal (terminal de punto de
venta operado por el empleado)._

### 6. POSTCONDICIONES
1. La acumulación queda registrada en la tabla `Movimientos`, disponible para
   auditoría (**RN-13**).
2. El saldo de puntos del cliente queda actualizado: los puntos nuevos suman al
   saldo disponible recién cuando pasan los 15 días de pendiente (**N2**).

### 7. DETALLE DE IMPLEMENTACIÓN

| Regla / Validación | Dónde se aplica | Respuesta |
| --- | --- | --- |
| Campos obligatorios (`clienteId`, ítems), cantidad 1..1000 | DTO (`AcumulacionCreateDTO`, `ItemCompraDTO`) | 400 |
| Formato del DNI en la búsqueda (7 a 10 dígitos) | `ClienteService.BuscarPorDocumentoAsync` (antes de consultar la base) | 400 |
| Cliente existente (por id) / activo | `MovimientoService` | 404 / 409 |
| Producto existente y activo (RN-26) | `MovimientoService.CalcularMontoAsync` | 400 |
| RN-12: regla vigente | `ReglaAcumulacionRepository.GetVigenteAsync` (la de `vigenciaDesde` más reciente que ya empezó) | 409 si no hay |
| **N1**: Gold x1.5 | `MovimientoService` (`Cliente.Categoria`) | — |
| **N2**: pendiente 15 días | `MovimientoService` (`FechaDisponible`) + filtro en `MovimientoRepository` | — |
| RN-13: auditoría | El movimiento guarda fecha, puntos y el detalle del cálculo | — |

* **Ejemplo:** compra de 2 productos de $1000 con la regla sembrada (0.1 pts/$):
  cliente Estándar → 200 puntos; cliente Gold → 300 puntos. En ambos casos quedan
  pendientes 15 días.
* **El id es la clave, no el DNI:** el DNI solo se usa para **buscar** al cliente
  (`GET /api/clientes?documento={dni}`, roles Empleado y Admin). La búsqueda recibe un
  `ClienteBusquedaDTO` por `[FromQuery]` y responde un `ClienteBusquedaResponseDTO`
  reducido (`id`, `nombre`, `documento`, `categoria`), sin email ni teléfono. La compra
  se registra con ese `clienteId` (Guid), que es además la clave foránea del movimiento.
* **Datos de prueba:** el seed incluye un cliente Gold (`40111222`,
  `cliente.gold@fidelix.local` / `Gold1234!`). Su id se genera al sembrar, por eso en
  Bruno primero se busca por DNI ("Buscar Cliente Gold por DNI") y el id queda en la
  variable `clienteGoldId`.
* **Autenticación y RF-28:** el endpoint exige el token del login de empleado (CU-09,
  `[Authorize(Roles = "Empleado")]`); el movimiento guarda el `EmpleadoId` del token, y la
  operación queda además en la auditoría (CU-21).
* **Colección Bruno:** `bruno/Movimientos/` y la búsqueda en `bruno/Clientes/` ("Buscar Cliente por DNI").

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del movimiento de acumulación. |
| `200` | OK | Búsqueda por DNI exitosa (paso previo). |
| `400` | Bad Request | Ítem de venta inválido, `clienteId` faltante o inválido, o DNI mal ingresado en la búsqueda. |
| `404` | Not Found | DNI no registrado (búsqueda) o `clienteId` que no corresponde a ningún cliente. |
| `409` | Conflict | Cliente inactivo o sin regla de acumulación vigente. |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Matriz de trazabilidad CU-10 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `RegistrarAcumulacionAsync_WithValidPurchase_AccruesPointsPerRulesAndLogsAudit` | `RegistrarAcumulacion_WithValidData_Returns201Created` |
| Flujo principal, N1 (Gold x1.5) | `201 Created` | `RegistrarAcumulacionAsync_WithGoldCliente_AppliesMultiplier` | `RegistrarAcumulacion_WithGoldCliente_AppliesMultiplier` |
| 2a. Ítem inválido (cantidad) | `400 Bad Request` | — (validación de esquema) | `RegistrarAcumulacion_WithInvalidItem_Returns400BadRequest` |
| 2a. Producto inactivo (RN-26) | `400 Bad Request` | `RegistrarAcumulacionAsync_WithInactiveProducto_ThrowsItemCompraInvalidoException` | — (cubierto por el unitario) |
| 1. Búsqueda por DNI | `200 OK` | `BuscarPorDocumentoAsync_WithExistingDocumento_ReturnsReducedCliente` | `BuscarPorDocumento_WithExistingDocumento_Returns200WithReducedData`, `BuscarPorDocumento_AsAdmin_Returns200OK` |
| 1a. DNI no registrado | `404 Not Found` | `BuscarPorDocumentoAsync_WithUnknownDocumento_ThrowsClienteNotFoundException` | `BuscarPorDocumento_WithUnknownDocumento_Returns404NotFound` |
| 1b. DNI mal ingresado | `400 Bad Request` | `BuscarPorDocumentoAsync_WithMalformedDocumento_ThrowsDocumentoInvalidoException` | `BuscarPorDocumento_WithMalformedDocumento_Returns400BadRequest`, `BuscarPorDocumento_WithoutDocumento_Returns400BadRequest` |
| 2a. `clienteId` faltante | `400 Bad Request` | — (validación de esquema) | `RegistrarAcumulacion_WithoutClienteId_Returns400BadRequest` |
| 3a. Cliente no registrado (id) | `404 Not Found` | `RegistrarAcumulacionAsync_WithUnknownClienteId_ThrowsClienteNotFoundException` | `RegistrarAcumulacion_WithUnknownClienteId_Returns404NotFound` |
| RN-11: solo personal busca clientes | `403 Forbidden` | — | `BuscarPorDocumento_AsCliente_Returns403Forbidden` |
| 3d. Sin regla vigente | `409 Conflict` | `RegistrarAcumulacionAsync_WithoutVigenteRule_ThrowsReglaAcumulacionNoVigenteException` | — (cubierto por el unitario) |
| 3c. Cálculo revisado antes de confirmar | Sin cambios (no se envía la petición) | — (verificación del empleado, previa al envío) | — (no genera una petición distinta; se cubre con el test del flujo principal una vez corregido el carrito) |
| 4a. Error interno de persistencia | `500 Internal Server Error` | `RegistrarAcumulacionAsync_WhenRepositoryFails_ThrowsPersistenceException` | `RegistrarAcumulacion_WhenPersistenceFails_Returns500InternalServerError` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
