# Caso de Uso: Consultar Beneficios Canjeados de Cliente (Empleado)

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Regla de negocio RN-10 (estado del canje visible) **a implementar**; cada caso
> borde debe contar con su test unitario e integración (ver matriz de
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
Permite al empleado consultar, mediante el documento (DNI) del cliente, el
listado de beneficios que canjeó previamente, con su fecha y estado.

### 2. PRECONDICIONES
1. El empleado se encuentra autenticado (Token JWT válido, CU-09).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `GET /api/clientes/{documento}/canjes`
   indicando el documento (DNI) del cliente a consultar.
2. La **Capa de Presentación** (`ClientesController.ConsultarCanjesPorDocumento`)
   valida que el documento tenga el formato correcto.
3. La **Capa de Negocio** (`ClienteService.ConsultarCanjesPorDocumentoAsync`)
   localiza al cliente por documento y recupera sus canjes, incluyendo el
   estado de cada uno (**RN-10**).
4. La **Capa de Persistencia** recupera los registros (`Canjes`) del cliente
   encontrado.
5. El Sistema devuelve un código **200 OK** con el listado de beneficios
   canjeados, su fecha y estado.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Cliente no encontrado por DNI (HTTP 404 Not Found):**
  1. El sistema detecta que en el Paso 3 el documento ingresado no corresponde a
     ningún cliente registrado.
  2. La Capa de Negocio no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found** con el mensaje: "El DNI
     ingresado es inválido". El flujo retorna al Paso 1.

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

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al recuperar el listado (con o sin resultados). |
| `404` | Not Found | DNI que no corresponde a ningún cliente registrado. |

### Matriz de trazabilidad CU-13 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `ConsultarCanjesPorDocumentoAsync_WithValidDocumento_ReturnsCanjesWithStatus` | `ConsultarCanjesPorDocumento_WithValidDocumento_Returns200OK` |
| 3a. Cliente no encontrado por DNI | `404 Not Found` | `ConsultarCanjesPorDocumentoAsync_WithUnknownDocumento_ThrowsClienteNotFoundException` | `ConsultarCanjesPorDocumento_WithUnknownDocumento_Returns404NotFound` |
| 4a. Sin canjes registrados | `200 OK` (lista vacía) | `ConsultarCanjesPorDocumentoAsync_WithNoCanjes_ReturnsEmptyList` | `ConsultarCanjesPorDocumento_WithNoCanjes_Returns200OKWithEmptyList` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
