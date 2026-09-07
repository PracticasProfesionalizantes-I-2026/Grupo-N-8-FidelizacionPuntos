# Caso de Uso: Consultar Movimientos

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Regla de negocio RN-06 (incluir acumulaciones y canjes) **a implementar**; cada
> caso borde debe contar con su test unitario e integración (ver matriz de
> trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-18 |
| **Nombre** | Consultar movimientos |
| **Actor Principal** | Admin |
| **Alcance / Nivel** | Sistema; subfunción |
| **Stakeholders e intereses** | Admin → controlar la actividad global de puntos del sistema; Negocio → detectar anomalías en la acumulación o el canje |
| **Disparador (Trigger)** | El admin solicita consultar movimientos |
| **Prioridad / Frecuencia** | Media; uso frecuente |
| **Reglas de negocio relacionadas** | RN-06 (incluir acumulaciones y canjes) |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador consultar los movimientos de puntos (acumulaciones y
canjes) realizados en todo el sistema, con filtros de búsqueda.

### 2. PRECONDICIONES
1. El admin se encuentra autenticado (Token JWT válido, CU-13).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `GET /api/admin/movimientos` con
   filtros opcionales (`clienteId`, `tipoMovimiento`, `fechaDesde`, `fechaHasta`)
   como *query params*.
2. La **Capa de Presentación** (`AdminMovimientosController.Consultar`) valida el
   formato de los filtros recibidos.
3. La **Capa de Negocio** (`MovimientoService.ConsultarMovimientosAsync`) aplica
   los criterios de consulta, incluyendo tanto acumulaciones como canjes
   (**RN-06**).
4. La **Capa de Persistencia** recupera los movimientos (`Movimientos`)
   correspondientes.
5. El Sistema devuelve un código **200 OK** con los resultados.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Filtro con formato inválido (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 alguno de los filtros no respeta el formato esperado.
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **4a. Sin resultados (HTTP 200 OK):**
  1. El sistema detecta que en el Paso 4 el Sistema no encuentra movimientos para los filtros
     seleccionados.
  2. El Sistema no interrumpe el flujo: no es un error, es un resultado vacío
     válido.
  3. El Sistema devuelve un código **200 OK** con una lista vacía y el mensaje:
     "No existen resultados para los filtros seleccionados". Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: consulta de lectura simple, sin variantes de mecanismo relevantes._

### 6. POSTCONDICIONES
1. El admin visualiza los movimientos solicitados, sin que se modifique ningún
   dato persistente.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al recuperar los movimientos (con o sin resultados). |
| `400` | Bad Request | Filtro de búsqueda con formato inválido. |

### Matriz de trazabilidad CU-18 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `ConsultarMovimientosAsync_WithFilters_ReturnsAcumulacionesAndCanjes` | `ConsultarMovimientos_WithValidFilters_Returns200OK` |
| 2a. Filtro inválido | `400 Bad Request` | — (validación de esquema) | `ConsultarMovimientos_WithInvalidDateFilter_Returns400BadRequest` |
| 4a. Sin resultados | `200 OK` (lista vacía) | `ConsultarMovimientosAsync_WithNoMatches_ReturnsEmptyList` | `ConsultarMovimientos_WithNoMatches_Returns200OKWithEmptyList` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
