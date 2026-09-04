# Caso de Uso: Auditar Operaciones

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-20 (inmutabilidad de la auditoría) y RN-13 (registro de
> operaciones críticas) **a implementar**; cada caso borde debe contar con su test
> unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-20 |
| **Nombre** | Auditar operaciones |
| **Actor Principal** | Admin |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Admin → controlar y trazar las operaciones críticas del sistema; Negocio → contar con evidencia inalterable ante reclamos o discrepancias de puntos |
| **Disparador (Trigger)** | El admin solicita consultar auditoría |
| **Prioridad / Frecuencia** | Baja; uso ocasional |
| **Reglas de negocio relacionadas** | RN-20 (registros de auditoría inmutables); RN-13 (toda operación crítica se registra) |

---

### 1. BREVE DESCRIPCIÓN
Permite al admin consultar el historial de operaciones registradas en el sistema
para fines de control y seguimiento.

### 2. PRECONDICIONES
1. El admin se encuentra autenticado (Token JWT válido, CU-13).
2. Existen registros de auditoría previamente generados (**RN-13**).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `GET /api/admin/auditoria` con
   criterios de búsqueda (`fechaDesde`, `fechaHasta`, `tipoOperacion`, `actorId`)
   como *query params*.
2. La **Capa de Presentación** (`AdminAuditoriaController.Consultar`) valida el
   formato de los criterios recibidos.
3. La **Capa de Negocio** (`AuditoriaService.ConsultarAuditoriaAsync`) aplica los
   criterios de consulta sobre los registros, que son de solo lectura (**RN-20**).
4. La **Capa de Persistencia** recupera los registros (`Auditoria`)
   correspondientes.
5. El Sistema devuelve un código **200 OK** con los resultados; el admin puede
   luego consultar el detalle de una operación puntual mediante `GET
   /api/admin/auditoria/{id}`.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Consulta sin criterios de búsqueda (HTTP 200 OK):**
  1. Si en el Paso 3 el admin no ingresó ningún filtro (búsqueda completamente
     abierta).
  2. El Sistema, por política de la interfaz, no ejecuta una consulta masiva sin
     acotar.
  3. El Sistema devuelve un código **200 OK** con una lista vacía y solicita al
     admin definir al menos un criterio de búsqueda. Fin del caso de uso.

* **4a. Sin resultados (HTTP 200 OK):**
  1. Si en el Paso 4 el Sistema no encuentra registros para los filtros
     seleccionados.
  2. El Sistema no interrumpe el flujo: no es un error, es un resultado vacío
     válido.
  3. El Sistema devuelve un código **200 OK** con el mensaje: "No existen
     resultados para los filtros seleccionados". Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: consulta de lectura simple, sin variantes de mecanismo relevantes._

### 6. POSTCONDICIONES
1. El admin visualiza la información de auditoría solicitada.
2. Ningún registro de auditoría consultado es modificado ni eliminado (**RN-20**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al recuperar los registros de auditoría (con o sin resultados). |

### Matriz de trazabilidad CU-20 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `ConsultarAuditoriaAsync_WithFilters_ReturnsMatchingRecords` | `ConsultarAuditoria_WithValidFilters_Returns200OK` |
| 3a. Sin criterios de búsqueda | `200 OK` (lista vacía) | `ConsultarAuditoriaAsync_WithNoFilters_ReturnsEmptyList` | `ConsultarAuditoria_WithNoFilters_Returns200OKWithEmptyList` |
| 4a. Sin resultados | `200 OK` (lista vacía) | `ConsultarAuditoriaAsync_WithNoMatches_ReturnsEmptyList` | `ConsultarAuditoria_WithNoMatches_Returns200OKWithEmptyList` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
