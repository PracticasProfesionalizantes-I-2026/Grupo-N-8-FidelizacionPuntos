# Caso de Uso: Gestionar Acumulación de Puntos

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-12 (cálculo según reglas vigentes) y RN-13 (trazabilidad
> para auditoría) **a implementar**; cada caso borde debe contar con su test
> unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-10 |
| **Nombre** | Gestionar acumulación de puntos |
| **Actor Principal** | Empleado |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Empleado → registrar la venta y acreditar puntos correctamente; Cliente → recibir los puntos que le corresponden por su compra; Negocio → aplicar de forma consistente las reglas de acumulación vigentes |
| **Disparador (Trigger)** | El empleado registra una compra realizada por un cliente |
| **Prioridad / Frecuencia** | Alta; uso muy frecuente (cada venta) |
| **Reglas de negocio relacionadas** | RN-12 (cálculo según reglas vigentes); RN-13 (registro para auditoría) |

---

### 1. BREVE DESCRIPCIÓN
Permite al empleado registrar una compra de un cliente y acreditarle los puntos
correspondientes según las reglas de acumulación configuradas (CU-17).

### 2. PRECONDICIONES
1. El empleado se encuentra autenticado (Token JWT válido, CU-09).
2. El cliente se encuentra registrado en el sistema.
3. Existen reglas de acumulación activas (CU-17).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/movimientos/acumulaciones`
   con un JSON que contiene el documento del cliente y el detalle de la compra
   (`clienteDocumento`, `items: [{producto, cantidad, monto}]`).
2. La **Capa de Presentación** (`MovimientosController.RegistrarAcumulacion`)
   valida que el JSON sea estructuralmente correcto.
3. La **Capa de Negocio** (`MovimientoService.RegistrarAcumulacionAsync`) localiza
   al cliente por documento y calcula la cantidad de puntos a acreditar aplicando
   la regla de acumulación vigente (**RN-12**).
4. La **Capa de Persistencia** registra el movimiento de acumulación
   (`Movimientos`) para fines de auditoría (**RN-13**) y actualiza el saldo
   disponible del cliente.
5. El Sistema devuelve un código **201 Created** con la confirmación de la
   operación (puntos acreditados y nuevo saldo).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Dato obligatorio faltante o producto/cantidad inválido (HTTP 400 Bad
  Request):**
  1. El sistema detecta que en el Paso 2 el empleado ingresa un producto o cantidad incorrectos; el
     empleado puede corregir/quitar el ítem mal cargado antes de reenviar la
     petición sin que el caso de uso finalice.
  2. El Sistema (Capa de Presentación) rechaza únicamente la petición con el ítem
     inválido, no la venta completa.
  3. El Sistema devuelve un código **400 Bad Request** detallando el ítem
     rechazado. El flujo retorna al Paso 1.

* **3a. Cliente no registrado (HTTP 404 Not Found):**
  1. El sistema detecta que en el Paso 3 el documento ingresado no corresponde a ningún cliente
     registrado.
  2. La Capa de Negocio no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found** con el mensaje: "El documento
     ingresado no se encuentra registrado". Fin del caso de uso.

* **3b. Documento del cliente mal ingresado (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 3 el documento no respeta el formato válido (ej. longitud o
     tipo de dato incorrectos).
  2. El Sistema (Capa de Negocio) rechaza la operación antes de buscar al cliente.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: "El
     documento ingresado fue incorrecto". Fin del caso de uso.

* **4a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. El sistema detecta que en el Paso 4 la Capa de Persistencia no puede registrar el movimiento o
     actualizar el saldo.
  2. El Sistema revierte la transacción y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de
     uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: la acumulación se registra por un único canal (terminal de punto de
venta operado por el empleado)._

### 6. POSTCONDICIONES
1. La acumulación queda registrada en la tabla `Movimientos`, disponible para
   auditoría (**RN-13**).
2. El saldo de puntos del cliente queda actualizado.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del movimiento de acumulación. |
| `400` | Bad Request | Ítem de venta inválido o documento del cliente mal ingresado. |
| `404` | Not Found | Documento que no corresponde a ningún cliente registrado. |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Matriz de trazabilidad CU-10 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `RegistrarAcumulacionAsync_WithValidPurchase_AccruesPointsPerRulesAndLogsAudit` | `RegistrarAcumulacion_WithValidData_Returns201Created` |
| 2a. Ítem inválido | `400 Bad Request` | — (validación de esquema) | `RegistrarAcumulacion_WithInvalidItem_Returns400BadRequest` |
| 3a. Cliente no registrado | `404 Not Found` | `RegistrarAcumulacionAsync_WithUnknownDocumento_ThrowsClienteNotFoundException` | `RegistrarAcumulacion_WithUnknownDocumento_Returns404NotFound` |
| 3b. Documento mal ingresado | `400 Bad Request` | `RegistrarAcumulacionAsync_WithMalformedDocumento_ThrowsValidationException` | `RegistrarAcumulacion_WithMalformedDocumento_Returns400BadRequest` |
| 4a. Error interno de persistencia | `500 Internal Server Error` | `RegistrarAcumulacionAsync_WhenRepositoryFails_ThrowsPersistenceException` | `RegistrarAcumulacion_WhenPersistenceFails_Returns500InternalServerError` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
