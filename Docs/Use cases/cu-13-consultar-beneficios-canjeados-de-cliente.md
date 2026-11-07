# Caso de Uso: Consultar Beneficios Canjeados de Cliente (Empleado)

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Regla de negocio RN-10 (estado del canje visible) **implementada**; cada caso
> borde cuenta con su test unitario e integración (ver matriz de
> trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-13 |
| **Nombre** | Consultar beneficios canjeados de un cliente (empleado) |
| **Actor Principal** | Empleado |
| **Alcance / Nivel** | Sistema; subfunción |
| **Stakeholders e intereses** | Empleado → confirmar en el local qué beneficios tiene pendientes de entrega un cliente puntual; Cliente → que el empleado pueda verificar sus canjes al momento de retirar un beneficio |
| **Disparador (Trigger)** | El empleado solicita ver los beneficios canjeados de un cliente en particular |
| **Prioridad / Frecuencia** | Media; uso frecuente |
| **Reglas de negocio relacionadas** | RN-10 (estado del canje visible: pendiente/entregado) |

---

### 1. BREVE DESCRIPCIÓN
Permite al empleado consultar el listado de beneficios que un cliente canjeó
previamente, con su fecha y estado. El empleado ubica al cliente por su documento
(DNI) y la consulta se hace con el **id** que devuelve esa búsqueda.

### 2. PRECONDICIONES
1. El empleado se encuentra autenticado (Token JWT válido, CU-09).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor busca al cliente por documento (`GET /api/clientes?documento={dni}`, ver
   CU-10), obtiene su id y envía una petición al endpoint `GET /api/clientes/{id}/canjes`.
2. La **Capa de Presentación** (`ClientesController.ConsultarCanjesDeCliente`)
   valida que el id sea un número entero (restricción de ruta `{id:int}`).
3. La **Capa de Negocio** (`ClienteService.ConsultarCanjesDeClienteAsync`)
   localiza al cliente por su id y recupera sus canjes, incluyendo el
   estado de cada uno (**RN-10**).
4. La **Capa de Persistencia** recupera los movimientos de tipo `Canje` del cliente
   encontrado.
5. El Sistema devuelve un código **200 OK** con el listado de beneficios
   canjeados, su fecha y estado.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. DNI mal ingresado o no registrado en la búsqueda (HTTP 400 / 404):**
  1. El sistema detecta que en el Paso 1 el documento no tiene formato válido (7 a 10
     dígitos) o no corresponde a ningún cliente.
  2. El Sistema devuelve **400 Bad Request** ("El documento ingresado fue incorrecto.")
     o **404 Not Found** ("El documento ingresado no se encuentra registrado."). El
     flujo retorna al Paso 1.

* **3a. Cliente no encontrado (HTTP 404 Not Found):**
  1. El sistema detecta que en el Paso 3 el id enviado no corresponde a
     ningún cliente registrado.
  2. La Capa de Negocio no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found** con el mensaje: "El cliente
     no se encuentra registrado.". El flujo retorna al Paso 1.

* **4a. Sin canjes registrados (HTTP 200 OK):**
  1. El sistema detecta que en el Paso 4 el Sistema no encuentra canjes realizados por el cliente
     encontrado.
  2. El Sistema no interrumpe el flujo: no es un error, es un resultado vacío
     válido.
  3. El Sistema devuelve un código **200 OK** con una lista vacía y el mensaje:
     "El cliente aún no registra beneficios canjeados". Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: consulta de lectura simple, realizada desde la terminal del
empleado (punto de venta)._

### 6. POSTCONDICIONES
1. El empleado visualiza el historial de canjes del cliente consultado, sin que
   se modifique ningún dato persistente.

### 7. DETALLE DE IMPLEMENTACIÓN

| Endpoint | Rol (token JWT) | Controller → Service |
| --- | --- | --- |
| `GET /api/clientes?documento={dni}` (paso previo) | Empleado, Admin | `ClientesController.BuscarPorDocumento → ClienteService.BuscarPorDocumentoAsync` |
| `GET /api/clientes/{id}/canjes` | Empleado | `ClientesController.ConsultarCanjesDeCliente → ClienteService.ConsultarCanjesDeClienteAsync` |

* **El id es la clave, no el DNI:** la ruta usa el id (entero) del cliente; el DNI solo se usa en la búsqueda previa. Reemplaza a la ruta anterior `GET /api/clientes/{documento}/canjes`.
* **Colección Bruno:** `bruno/Empleado/` ("Buscar Cliente por DNI", "Canjes de Cliente", "Canjes de Cliente - No Encontrado").

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al recuperar el listado (con o sin resultados). |
| `400` | Bad Request | DNI mal ingresado en la búsqueda. |
| `404` | Not Found | DNI no registrado (búsqueda) o id que no corresponde a ningún cliente. |

### Matriz de trazabilidad CU-13 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `ConsultarCanjesDeClienteAsync_WithExistingId_ReturnsCanjesWithStatus` | `ConsultarCanjesDeCliente_WithExistingId_Returns200OK` |
| 1a. DNI mal ingresado / no registrado | `400` / `404` | `BuscarPorDocumentoAsync_WithMalformedDocumento_ThrowsDocumentoInvalidoException`, `BuscarPorDocumentoAsync_WithUnknownDocumento_ThrowsClienteNotFoundException` | `BuscarPorDocumento_WithMalformedDocumento_Returns400BadRequest`, `BuscarPorDocumento_WithUnknownDocumento_Returns404NotFound` |
| 3a. Cliente no encontrado (id) | `404 Not Found` | `ConsultarCanjesDeClienteAsync_WithUnknownId_ThrowsClienteNotFoundException` | `ConsultarCanjesDeCliente_WithUnknownId_Returns404NotFound` |
| 4a. Sin canjes registrados | `200 OK` (lista vacía) | `ConsultarCanjesDeClienteAsync_WithNoCanjes_ReturnsEmptyList` | `ConsultarCanjesDeCliente_WithNoCanjes_Returns200OKWithEmptyList` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
