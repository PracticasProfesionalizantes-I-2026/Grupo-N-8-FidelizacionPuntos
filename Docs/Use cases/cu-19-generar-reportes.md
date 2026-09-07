# Caso de Uso: Generar Reportes

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Regla de negocio RN-19 (consistencia con auditoría) **a implementar**; cada caso
> borde debe contar con su test unitario e integración (ver matriz de
> trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-19 |
| **Nombre** | Generar reportes |
| **Actor Principal** | Admin |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Admin → tomar decisiones comerciales con información agregada; Negocio → medir el impacto real del programa de fidelización |
| **Disparador (Trigger)** | El admin solicita generar un reporte |
| **Prioridad / Frecuencia** | Baja; uso ocasional (períodos de análisis) |
| **Reglas de negocio relacionadas** | RN-19 (reportes consistentes con los registros de auditoría) |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador generar reportes sobre la actividad del sistema
(acumulación, canjes, clientes activos, etc.) para un período determinado.

### 2. PRECONDICIONES
1. El admin se encuentra autenticado (Token JWT válido, CU-13).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `GET /api/admin/reportes` indicando el
   tipo de reporte y parámetros (`tipo`, `periodoDesde`, `periodoHasta`) como
   *query params*.
2. La **Capa de Presentación** (`AdminReportesController.Generar`) valida el
   formato de los parámetros recibidos.
3. La **Capa de Negocio** (`ReporteService.GenerarReporteAsync`) procesa la
   información solicitada, garantizando que sea consistente con los registros de
   auditoría (**RN-19**).
4. La **Capa de Persistencia** recupera los datos agregados necesarios (a partir
   de `Movimientos`, `Canjes` y `Auditoria`).
5. El Sistema genera el reporte y devuelve un código **200 OK** con el contenido
   (o un enlace de descarga).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Parámetros inválidos (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 el tipo de reporte no existe o el rango de período es
     inválido (ej. `periodoDesde` posterior a `periodoHasta`).
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **4a. Sin datos para el período seleccionado (HTTP 200 OK):**
  1. El sistema detecta que en el Paso 4 el Sistema no encuentra información para los parámetros
     seleccionados.
  2. El Sistema no interrumpe el flujo: no es un error, es un resultado vacío
     válido.
  3. El Sistema devuelve un código **200 OK** con el mensaje: "No hay datos
     disponibles para el período seleccionado". Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El admin puede visualizar el reporte en pantalla o descargarlo (ej. CSV/PDF);
   en ambos casos el reporte generado y su contenido son idénticos.

### 6. POSTCONDICIONES
1. El reporte queda generado y disponible para el admin (visualización o
   descarga), sin que se modifique ningún dato persistente de origen.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al generar el reporte (con o sin datos). |
| `400` | Bad Request | Tipo de reporte inexistente o período inválido. |

### Matriz de trazabilidad CU-19 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `GenerarReporteAsync_WithValidParams_ReturnsConsistentReport` | `GenerarReporte_WithValidParams_Returns200OK` |
| 2a. Parámetros inválidos | `400 Bad Request` | — (validación de esquema) | `GenerarReporte_WithInvalidPeriodRange_Returns400BadRequest` |
| 4a. Sin datos para el período | `200 OK` (sin datos) | `GenerarReporteAsync_WithNoDataInPeriod_ReturnsEmptyReport` | `GenerarReporte_WithNoDataInPeriod_Returns200OKWithEmptyReport` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
