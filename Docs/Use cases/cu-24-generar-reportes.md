# Caso de Uso: Generar Reportes

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Regla de negocio RN-19 (consistencia con auditoría) **implementada** (ver alcance en la sección 7); cada caso
> borde cuenta con su test unitario e integración (ver matriz de
> trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-24 |
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
1. El admin se encuentra autenticado (Token JWT válido, CU-14).

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
   de las tablas `Movimientos` y `Auditorias`; los canjes son movimientos de tipo
   `Canje`).
5. El Sistema genera el reporte y devuelve un código **200 OK** con el contenido
   en pantalla (JSON) o como archivo CSV descargable.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Parámetros inválidos (HTTP 400 Bad Request):**
  1. El sistema detecta que en el Paso 2 el tipo de reporte o el formato no existen, falta un
     parámetro obligatorio o el rango de período es inválido (ej. `periodoDesde` posterior a
     `periodoHasta`). En la comparación, además: el segundo período incompleto o pedido sin
     indicador, o un período de más de 366 días.
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
1. El admin puede visualizar el reporte en pantalla (`formato=Json`, por defecto) o
   descargarlo como CSV (`formato=Csv`); en ambos casos el reporte generado y su
   contenido son idénticos. PDF queda fuera del alcance de esta versión.
2. **Comparación del dashboard** ("Tipo 1 vs Tipo 2" del prototipo, también pedida
   en CU-20): `GET /api/admin/reportes/comparacion` compara, día por día, los
   indicadores **Ventas** (cantidad de compras), **Canjes** (cantidad de canjes) y
   **PuntosOtorgados** (puntos de acumulaciones y bonos de cumpleaños):
   * **Misma fecha, distinto movimiento:** sin `indicador`, devuelve una serie por
     cada indicador en `periodo1`.
   * **Fechas distintas:** con `indicador`, devuelve una serie para `periodo1` y otra
     para `periodo2`. Como indica el prototipo, el segundo período solo se puede usar
     cuando se elige un único indicador.

### 6. POSTCONDICIONES
1. El reporte queda generado y disponible para el admin (visualización o
   descarga), sin que se modifique ningún dato persistente de origen.

### 7. DETALLE DE IMPLEMENTACIÓN

| Endpoint | Rol (token JWT) | Controller → Service |
| --- | --- | --- |
| `GET /api/admin/reportes?tipo&periodoDesde&periodoHasta` | Admin | `AdminReportesController.Generar → ReporteService.GenerarReporteAsync` |
| `GET /api/admin/reportes?tipo&periodoDesde&periodoHasta&formato=Csv` (descarga) | Admin | `AdminReportesController.Generar → ReporteService.GenerarReporteAsync` + `ReporteCsvWriter` |
| `GET /api/admin/reportes/comparacion?indicador&periodo1Desde&periodo1Hasta&periodo2Desde&periodo2Hasta` | Admin | `AdminReportesController.Comparar → ReporteService.CompararAsync` |

* Tipos: `Resumen` (puntos acumulados, de bono, canjeados y vencidos; clientes con movimientos) y `BeneficiosMasCanjeados` (además, el ranking).
* **RN-19 (alcance):** el reporte se calcula exclusivamente sobre los movimientos registrados, que son inmutables y son los mismos que se auditan; por eso sus totales coinciden con los de la consulta de movimientos (CU-20) para el mismo período. El campo `registrosAuditoria` es **solo informativo**: cuenta *todos* los registros de auditoría del período (de cualquier operación: altas de productos, logins de personal, procesos, etc.), **no** es una comparación uno a uno con los movimientos ni una verificación automática de consistencia. Sirve como referencia para que el admin contraste manualmente con la auditoría (CU-21).
* **Fechas en hora Argentina:** los períodos se interpretan en hora Argentina (UTC-3) y se convierten a UTC con `FechasNegocio` (`BusinessLogic/Helpers`), el mismo helper que usan CU-05 y CU-20. Si `periodoHasta` no trae hora, se incluye el día completo. La zona se resuelve en Linux, macOS y Windows (id IANA, id de Windows o, si el sistema no tiene la base de zonas, UTC-3 fijo: Argentina no tiene horario de verano).
* **CSV:** lo arma `ReporteCsvWriter` (capa API, `API/Formatters`) a partir del mismo `ReporteResponseDTO` que se muestra en pantalla: filas `Campo,Valor` con los totales y, en `BeneficiosMasCanjeados`, la tabla del ranking. Separador coma (RFC 4180), UTF-8 con BOM, fechas `yyyy-MM-dd HH:mm:ss`. Se descarga como `reporte-{tipo}-{desde}-{hasta}.csv`.
* **Comparación:** cada serie trae `indicador`, `periodoDesde`, `periodoHasta`, `total` y `valores` (un punto `{ fecha, valor }` por día, incluidos los días en 0, para que el gráfico no tenga huecos). Los movimientos se agrupan por su día en hora Argentina. Si todas las series dan 0, `mensaje` = "No hay datos disponibles para el período seleccionado". "Ventas" es la cantidad de compras: el monto en pesos no se guarda en una columna propia.
* Los parámetros se validan en los DTO (`ReporteQueryDTO`, `ComparacionQueryDTO`, `IValidatableObject`) → 400.
* **Colección Bruno:** `bruno/Admin/Consultas/` (`Reporte Resumen`, `Reporte Beneficios Mas Canjeados`, `Reporte - Periodo Invalido`, `Reporte Resumen CSV`, `Reporte - Formato Invalido`, `Comparacion - Indicadores`, `Comparacion - Dos Periodos`, `Comparacion - Segundo Periodo Sin Indicador`, `Comparacion - Periodo Invalido`).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al generar el reporte o la comparación (con o sin datos), en JSON o CSV. |
| `400` | Bad Request | Tipo, formato o indicador inexistente, parámetro faltante o período inválido. |
| `401` / `403` | Unauthorized / Forbidden | Sin token, o con token de un rol que no es Admin (RN-11). |

### Matriz de trazabilidad CU-24 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `GenerarReporteAsync_WithValidParams_ReturnsConsistentReport` | `GenerarReporte_WithValidParams_Returns200OK` |
| Paso 3: período en hora Argentina (día completo) | `200 OK` | `GenerarReporteAsync_WithDateOnlyEnd_IncludesWholeLastDay`, `GenerarReporteAsync_QueriesPeriodInUtcFromArgentineTime` | `GenerarReporte_ForSameDay_MatchesConsultarMovimientos` |
| 2a. Parámetros inválidos | `400 Bad Request` | — (validación de esquema) | `GenerarReporte_WithInvalidPeriodRange_Returns400BadRequest`, `GenerarReporte_WithUnknownType_Returns400BadRequest`, `GenerarReporte_WithUnknownFormat_Returns400BadRequest` |
| 4a. Sin datos para el período | `200 OK` (sin datos) | `GenerarReporteAsync_WithNoDataInPeriod_ReturnsEmptyReport` | `GenerarReporte_WithNoDataInPeriod_Returns200OKWithEmptyReport` |
| Sub-variación 1: descarga CSV | `200 OK` (`text/csv`) | — (capa API) | `GenerarReporte_WithCsvFormat_ReturnsSameContentAsJson` |
| Sub-variación 2: misma fecha, distinto movimiento | `200 OK` | `CompararAsync_WithoutIndicador_ReturnsOneSeriesPerIndicador` | `CompararIndicadores_WithoutIndicador_Returns200WithThreeSeries` |
| Sub-variación 2: fechas distintas | `200 OK` | `CompararAsync_WithIndicadorAndTwoPeriods_ReturnsOneSeriesPerPeriod` | `CompararIndicadores_WithIndicadorAndTwoPeriods_Returns200WithTwoSeries` |
| Sub-variación 2: venta después de las 21 h (hora AR) | `200 OK` | `CompararAsync_WithSaleAfter21ArgentineTime_CountsOnArgentineDay` | — |
| Sub-variación 2, 2a: segundo período sin indicador / período invertido | `400 Bad Request` | — (validación de esquema) | `CompararIndicadores_SecondPeriodWithoutIndicador_Returns400BadRequest`, `CompararIndicadores_WithInvertedPeriod_Returns400BadRequest` |
| Sub-variación 2, 4a: sin datos | `200 OK` (con mensaje) | `CompararAsync_WithNoData_ReturnsMessage` | — |
| RN-11: rol no admin | `403 Forbidden` | — | `CompararIndicadores_AsCliente_Returns403Forbidden` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
