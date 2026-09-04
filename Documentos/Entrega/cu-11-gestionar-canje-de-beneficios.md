# Caso de Uso: Gestionar Canje de Beneficios

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-08 (saldo suficiente), RN-09 (método FIFO), RN-17
> (beneficio activo) y RN-13 (trazabilidad para auditoría) **a implementar**; cada
> caso borde debe contar con su test unitario e integración (ver matriz de
> trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-11 |
| **Nombre** | Gestionar canje de beneficios |
| **Actor Principal** | Empleado |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Empleado → entregar el beneficio correcto descontando los puntos del cliente; Cliente → canjear presencialmente sin usar la app; Negocio → mantener el mismo control de saldo y FIFO que el canje por app |
| **Disparador (Trigger)** | El cliente solicita canjear un beneficio de forma presencial |
| **Prioridad / Frecuencia** | Alta; uso frecuente |
| **Reglas de negocio relacionadas** | RN-08 (saldo suficiente); RN-09 (método FIFO); RN-17 (beneficio activo); RN-13 (registro para auditoría) |

---

### 1. BREVE DESCRIPCIÓN
Permite al empleado realizar, en el punto de venta, el canje de un beneficio en
nombre de un cliente que se presenta físicamente en el local.

### 2. PRECONDICIONES
1. El empleado se encuentra autenticado (Token JWT válido, CU-09).
2. El cliente se encuentra registrado y posee puntos disponibles.
3. El beneficio se encuentra activo (**RN-17**).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/canjes` con un JSON que
   contiene el documento del cliente y el beneficio elegido (`clienteDocumento`,
   `beneficioId`).
2. La **Capa de Presentación** (`CanjesController.CrearCanjePresencial`) valida
   que el JSON sea estructuralmente correcto y localiza el saldo de puntos del
   cliente por su documento.
3. La **Capa de Negocio** (`CanjeService.CrearCanjePresencialAsync`) verifica que
   el beneficio esté activo (**RN-17**) y que el cliente posea puntos suficientes
   (**RN-08**).
4. El Sistema calcula los puntos a descontar utilizando el método **FIFO**
   (**RN-09**, ver CU-22).
5. La **Capa de Persistencia** registra el canje (con auditoría, **RN-13**) y
   actualiza el saldo de puntos del cliente.
6. El Sistema devuelve un código **201 Created** confirmando la operación al
   empleado.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. Cliente no registrado (HTTP 404 Not Found):**
  1. Si en el Paso 1/2 el documento ingresado no corresponde a un cliente
     registrado.
  2. La Capa de Negocio no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found** con el mensaje: "El documento
     ingresado no se encuentra registrado". Fin del caso de uso.

* **3a. Beneficio inactivo (HTTP 409 Conflict):**
  1. Si en el Paso 3 el beneficio seleccionado no está activo, violando **RN-17**.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `BeneficioInactivoException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "El beneficio
     no pudo ser canjeado". Fin del caso de uso.

* **3b. Saldo insuficiente (HTTP 409 Conflict):**
  1. Si en el Paso 3 el cliente no posee puntos suficientes, violando **RN-08**.
  2. El Sistema (Capa de Negocio) lanza la excepción de dominio
     `SaldoInsuficienteException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "Saldo
     insuficiente para hacer canje". El flujo retorna al Paso 3 para que el
     empleado seleccione otro beneficio.

* **5a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 5 la Capa de Persistencia no puede registrar el canje o
     actualizar el saldo.
  2. El Sistema revierte la transacción y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de
     uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: el canje presencial se realiza por un único canal (terminal de punto
de venta operado por el empleado)._

### 6. POSTCONDICIONES
1. El canje queda registrado en la tabla `Canjes`, disponible para auditoría
   (**RN-13**).
2. El saldo de puntos del cliente queda actualizado según el consumo FIFO.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del nuevo recurso Canje. |
| `404` | Not Found | Documento que no corresponde a ningún cliente registrado. |
| `409` | Conflict | Violación de RN-17 (beneficio inactivo) o RN-08 (saldo insuficiente). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Matriz de trazabilidad CU-11 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `CrearCanjePresencialAsync_WithSufficientBalance_CreatesCanjeAndLogsAudit` | `CrearCanjePresencial_WithValidData_Returns201Created` |
| 1a. Cliente no registrado | `404 Not Found` | `CrearCanjePresencialAsync_WithUnknownDocumento_ThrowsClienteNotFoundException` | `CrearCanjePresencial_WithUnknownDocumento_Returns404NotFound` |
| 3a. Beneficio inactivo | `409 Conflict` | `CrearCanjePresencialAsync_WithInactiveBeneficio_ThrowsBeneficioInactivoException` | `CrearCanjePresencial_WithInactiveBeneficio_Returns409Conflict` |
| 3b. Saldo insuficiente | `409 Conflict` | `CrearCanjePresencialAsync_WithInsufficientBalance_ThrowsSaldoInsuficienteException` | `CrearCanjePresencial_WithInsufficientBalance_Returns409Conflict` |
| 5a. Error interno de persistencia | `500 Internal Server Error` | `CrearCanjePresencialAsync_WhenRepositoryFails_ThrowsPersistenceException` | `CrearCanjePresencial_WhenPersistenceFails_Returns500InternalServerError` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. El cálculo
> FIFO se cubre en detalle en la matriz de CU-22.
