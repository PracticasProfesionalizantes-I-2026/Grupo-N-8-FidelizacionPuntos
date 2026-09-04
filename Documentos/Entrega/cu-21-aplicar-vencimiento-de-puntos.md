# Caso de Uso: Aplicar Vencimiento de Puntos

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), adaptada a un caso de uso de
> **Sistema** (proceso interno programado, sin actor humano ni endpoint HTTP
> público). Reglas de negocio RN-09 (FIFO) y RN-13 (registro para auditoría) **a
> implementar**; cada caso borde debe contar con su test unitario e integración
> (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-21 |
| **Nombre** | Aplicar vencimiento de puntos |
| **Actor Principal** | Sistema |
| **Alcance / Nivel** | Sistema; subfunción (proceso batch interno) |
| **Stakeholders e intereses** | Sistema → mantener saldos correctos según la vigencia real de los puntos; Cliente → ser notificado cuando pierde puntos por vencimiento; Negocio → que el vencimiento se aplique de forma consistente y auditable |
| **Disparador (Trigger)** | Se cumple el proceso periódico (programado) de verificación de vencimientos |
| **Prioridad / Frecuencia** | Alta; frecuencia diaria (job programado) |
| **Reglas de negocio relacionadas** | RN-09 (método FIFO); RN-13 (registro para auditoría) |

---

### 1. BREVE DESCRIPCIÓN
Permite al sistema identificar y descontar automáticamente los puntos que
alcanzaron su fecha de vencimiento, siguiendo el orden FIFO.

### 2. PRECONDICIONES
1. Existen lotes de puntos acumulados con fecha de vencimiento definida en la
   Capa de Persistencia.

### 3. FLUJO PRINCIPAL (Camino Feliz - Proceso interno)
> Nota: este CU no expone un endpoint HTTP público; se ejecuta como *job*
> programado dentro de la Capa de Negocio. Se documenta el resultado del proceso
> (éxito / error interno) en lugar de un código de respuesta HTTP.
1. El *job* programado `PointsExpirationJob` invoca al Sistema (Capa de Negocio,
   `PuntosService.AplicarVencimientoAsync`) según la frecuencia configurada.
2. La **Capa de Negocio** identifica, para cada cliente, los lotes de puntos cuya
   fecha de vencimiento ya se cumplió, respetando el orden **FIFO** (**RN-09**: el
   lote más antiguo vence primero).
3. La **Capa de Negocio** descuenta los puntos vencidos del saldo del cliente
   correspondiente.
4. La **Capa de Persistencia** registra el movimiento de vencimiento en
   `Movimientos` para fines de auditoría (**RN-13**).
5. El Sistema notifica al cliente sobre los puntos vencidos (ver CU-23) y el
   proceso finaliza con estado **Éxito**.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. No hay puntos vencidos (Resultado: Sin cambios):**
  1. Si en el Paso 2 el Sistema no encuentra lotes de puntos vencidos.
  2. El proceso finaliza sin realizar cambios ni registrar movimientos.
  3. El *job* registra en su log de ejecución: "Sin vencimientos a aplicar". Fin
     del caso de uso.

* **4a. Error interno en la persistencia (Resultado: Error):**
  1. Si en el Paso 4 la Capa de Persistencia no puede registrar el movimiento de
     vencimiento (ej. falla de conexión).
  2. El Sistema interrumpe el descuento de ese cliente, registra el error como no
     controlado y continúa con el siguiente cliente del lote (el fallo de un
     cliente no bloquea el proceso completo).
  3. El *job* queda registrado con estado **Error parcial**, disponible para
     reintento o revisión manual del admin (CU-20). Fin del caso de uso para ese
     cliente.

### 5. SUB-VARIACIONES (opcional)
_No aplica: el proceso se ejecuta por un único mecanismo (job programado
interno)._

### 6. POSTCONDICIONES
1. El saldo de puntos de cada cliente afectado queda actualizado, descontando los
   lotes vencidos.
2. El vencimiento queda registrado en `Movimientos` y es auditable (**RN-13**,
   CU-20).

---

## Anexo: matrices de referencia

### Resultado del proceso (no aplica tabla de códigos HTTP — proceso interno)

| Resultado | Contexto de Aplicación en el Caso de Uso |
| --- | --- |
| `Éxito` | Vencimientos detectados, aplicados y auditados correctamente. |
| `Sin cambios` | No se encontraron lotes vencidos en la ejecución del job. |
| `Error parcial` | Falla de persistencia al procesar un cliente puntual; el proceso continúa con el resto del lote. |

### Matriz de trazabilidad CU-21 → Test

| Paso del CU | Excepción / Resultado | Test unitario (BusinessLogic) | Test integración (Job / Persistencia) |
| --- | --- | --- | --- |
| Flujo principal | `Éxito` | `AplicarVencimientoAsync_WithExpiredLotes_DiscountsFifoAndLogsAudit` | `PointsExpirationJob_WithExpiredLotes_UpdatesSaldoAndNotifiesCliente` |
| 2a. No hay puntos vencidos | `Sin cambios` | `AplicarVencimientoAsync_WithNoExpiredLotes_MakesNoChanges` | `PointsExpirationJob_WithNoExpiredLotes_LogsNoChanges` |
| 4a. Error interno de persistencia | `Error parcial` | `AplicarVencimientoAsync_WhenRepositoryFailsForOneCliente_ContinuesWithRemainingClientes` | `PointsExpirationJob_WhenPersistenceFailsForOneCliente_LogsPartialError` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
