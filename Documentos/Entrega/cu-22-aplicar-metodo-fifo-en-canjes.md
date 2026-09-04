# Caso de Uso: Aplicar Método FIFO en Canjes

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), adaptada a un caso de uso de
> **Sistema** (subrutina interna invocada por otros casos de uso, sin actor
> humano ni endpoint HTTP propio). Reglas de negocio RN-09 (FIFO) y RN-21
> (exclusión de lotes vencidos) **a implementar**; cada caso borde debe contar con
> su test unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-22 |
| **Nombre** | Aplicar método FIFO en canjes |
| **Actor Principal** | Sistema |
| **Alcance / Nivel** | Sistema; subfunción (invocada internamente) |
| **Stakeholders e intereses** | Sistema → descontar siempre los puntos más antiguos primero; Cliente → que el descuento sea predecible y justo; Negocio → evitar inconsistencias en el consumo de lotes de puntos |
| **Disparador (Trigger)** | Se solicita un canje de beneficio (CU-07 o CU-11) |
| **Prioridad / Frecuencia** | Alta; se ejecuta en cada canje |
| **Reglas de negocio relacionadas** | RN-09 (método FIFO); RN-21 (excluir lotes vencidos) |

---

### 1. BREVE DESCRIPCIÓN
Permite al sistema determinar qué lotes de puntos deben descontarse al momento de
un canje, priorizando siempre los puntos vigentes más antiguos.

### 2. PRECONDICIONES
1. El cliente posee puntos suficientes para el canje (verificado previamente por
   CU-07/CU-11, **RN-08**).

### 3. FLUJO PRINCIPAL (Camino Feliz - Proceso interno)
> Nota: este CU no expone un endpoint HTTP propio; es invocado internamente desde
> la Capa de Negocio de CU-07 y CU-11 (`LotePuntosService.AplicarFifoAsync`), y
> hereda el código de respuesta HTTP del caso de uso que lo invoca.
1. El Sistema (Capa de Negocio) recupera los lotes de puntos vigentes del cliente,
   ordenados por fecha de acumulación (más antiguo primero), excluyendo los lotes
   ya vencidos (**RN-21**).
2. El Sistema descuenta puntos de los lotes en orden, comenzando por el más
   antiguo (**RN-09**).
3. El Sistema continúa descontando de los siguientes lotes hasta cubrir el total
   del canje.
4. La **Capa de Persistencia** actualiza el estado (puntos restantes) de cada
   lote afectado.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Un lote no cubre el total requerido (Continúa el flujo principal):**
  1. Si en el Paso 2 el lote más antiguo no alcanza para cubrir el total del
     canje.
  2. El Sistema descuenta el total disponible de ese lote y continúa con el
     siguiente lote (por antigüedad) hasta completar el monto del canje.
  3. El flujo continúa en el Paso 3 del flujo principal.

* **4a. Error interno en la persistencia (Resultado: Error, propagado a CU-07/11):**
  1. Si en el Paso 4 la Capa de Persistencia no puede actualizar el estado de los
     lotes.
  2. El Sistema revierte la operación completa (ningún lote queda parcialmente
     descontado) y propaga la excepción `PersistenceException` al caso de uso
     invocante (CU-07/CU-11).
  3. El caso de uso invocante responde **500 Internal Server Error**. Fin del caso
     de uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: el mecanismo de cálculo es único, independiente de si el canje se
originó en CU-07 (cliente) o CU-11 (empleado)._

### 6. POSTCONDICIONES
1. Los lotes de puntos del cliente quedan actualizados según el consumo FIFO.
2. Ningún lote vencido participa del descuento (**RN-21**).

---

## Anexo: matrices de referencia

### Resultado del proceso (no aplica tabla de códigos HTTP propia — hereda la de CU-07/CU-11)

| Resultado | Contexto de Aplicación en el Caso de Uso |
| --- | --- |
| `Éxito` | Lotes descontados correctamente en orden FIFO, excluyendo vencidos. |
| `Error` (propaga `500`) | Falla de persistencia al actualizar el estado de los lotes; se revierte toda la operación. |

### Matriz de trazabilidad CU-22 → Test

| Paso del CU | Excepción / Resultado | Test unitario (BusinessLogic) | Test integración (HTTP, vía CU-07/11) |
| --- | --- | --- | --- |
| Flujo principal | `Éxito` | `AplicarFifoAsync_WithSingleSufficientLote_DiscountsOldestLoteFirst` | `CrearCanje_WithSingleSufficientLote_Returns201Created` |
| 2a. Lote insuficiente, requiere múltiples lotes | `Éxito` (continúa) | `AplicarFifoAsync_WhenOldestLoteInsufficient_ContinuesWithNextLote` | `CrearCanje_WhenPointsSpanMultipleLotes_Returns201Created` |
| — Exclusión de lotes vencidos | `Éxito` | `AplicarFifoAsync_ExcludesExpiredLotesFromCalculation` | *(cubierto indirectamente por los tests de CU-21)* |
| 4a. Error interno de persistencia | `Error` (`500`) | `AplicarFifoAsync_WhenRepositoryFails_ThrowsPersistenceExceptionAndRollsBack` | `CrearCanje_WhenLoteUpdateFails_Returns500InternalServerError` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
