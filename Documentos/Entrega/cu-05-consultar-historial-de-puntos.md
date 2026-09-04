# Caso de Uso: Consultar Historial de Puntos

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Regla de negocio RN-06 (historial completo y cronológico) **a implementar**;
> cada caso borde debe contar con su test unitario e integración (ver matriz de
> trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-05 |
| **Nombre** | Consultar historial de puntos |
| **Actor Principal** | Cliente |
| **Alcance / Nivel** | Sistema; subfunción |
| **Stakeholders e intereses** | Cliente → entender cómo se generó y usó su saldo; Negocio → dar transparencia sobre el programa de fidelización |
| **Disparador (Trigger)** | El cliente solicita consultar su historial de puntos |
| **Prioridad / Frecuencia** | Media; uso frecuente |
| **Reglas de negocio relacionadas** | RN-06 (historial incluye acumulaciones y canjes, orden cronológico) |

---

### 1. BREVE DESCRIPCIÓN
Permite al cliente visualizar el detalle cronológico de las acumulaciones, canjes y
vencimientos de puntos realizados sobre su cuenta.

### 2. PRECONDICIONES
1. El actor debe poseer un estado de autenticación activo (Token JWT válido,
   CU-02).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `GET /api/clientes/me/movimientos` con
   filtros opcionales (`fechaDesde`, `fechaHasta`, `tipoMovimiento`) como *query
   params*.
2. La **Capa de Presentación** (`ClientesController.ConsultarHistorial`) valida el
   formato de los filtros recibidos.
3. La **Capa de Negocio** (`ClienteService.ConsultarHistorialAsync`) aplica los
   filtros y ordena los resultados cronológicamente, cumpliendo **RN-06**.
4. La **Capa de Persistencia** recupera los movimientos (`Movimientos`) del cliente
   correspondientes a los criterios indicados.
5. El Sistema devuelve un código **200 OK** con el historial de movimientos.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Filtro con formato inválido (HTTP 400 Bad Request):**
  1. Si en el Paso 2 alguno de los filtros (ej. `fechaDesde`) no respeta el formato
     esperado.
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **4a. Sin movimientos registrados (HTTP 200 OK):**
  1. Si en el Paso 4 el Sistema no encuentra movimientos para los criterios
     seleccionados.
  2. El Sistema no interrumpe el flujo: no es un error, es un resultado vacío
     válido.
  3. El Sistema devuelve un código **200 OK** con una lista vacía y el mensaje: "No
     hay movimientos para mostrar". Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El cliente puede consultar sin filtros (historial completo) o definiendo
   criterios de búsqueda (fecha, tipo de movimiento); el endpoint y el formato de
   respuesta son los mismos en ambos casos.

### 6. POSTCONDICIONES
1. El cliente visualiza el historial solicitado, sin que se modifique ningún dato
   persistente.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al recuperar el historial (con o sin resultados). |
| `400` | Bad Request | Filtro de búsqueda con formato inválido. |

### Matriz de trazabilidad CU-05 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `ConsultarHistorialAsync_WithFilters_ReturnsOrderedMovimientos` | `ConsultarHistorial_WithValidFilters_Returns200OK` |
| 2a. Filtro inválido | `400 Bad Request` | — (validación de esquema) | `ConsultarHistorial_WithInvalidDateFilter_Returns400BadRequest` |
| 4a. Sin movimientos | `200 OK` (lista vacía) | `ConsultarHistorialAsync_WithNoMovimientos_ReturnsEmptyList` | `ConsultarHistorial_WithNoMovimientos_Returns200OKWithEmptyList` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
