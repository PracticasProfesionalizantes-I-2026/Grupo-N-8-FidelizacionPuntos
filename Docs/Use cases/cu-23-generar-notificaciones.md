# Caso de Uso: Generar Notificaciones

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), adaptada a un caso de uso de
> **Sistema** (proceso interno disparado por eventos, sin actor humano ni endpoint
> HTTP público). Reglas de negocio RN-22 (antelación mínima) y RN-13 (registro
> para auditoría) **a implementar**; cada caso borde debe contar con su test
> unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-23 |
| **Nombre** | Generar notificaciones |
| **Actor Principal** | Sistema |
| **Alcance / Nivel** | Sistema; subfunción (proceso dirigido por eventos) |
| **Stakeholders e intereses** | Sistema → informar oportunamente al cliente sobre eventos relevantes de su cuenta; Cliente → enterarse de acumulaciones, canjes y vencimientos próximos; Negocio → aumentar el uso de puntos antes de que venzan |
| **Disparador (Trigger)** | Se produce un evento que requiere notificación (acumulación CU-10, canje CU-07/CU-11, vencimiento próximo CU-21) |
| **Prioridad / Frecuencia** | Alta; se ejecuta ante cada evento relevante |
| **Reglas de negocio relacionadas** | RN-22 (antelación mínima configurable); RN-13 (registro para auditoría) |

---

### 1. BREVE DESCRIPCIÓN
Permite al sistema enviar notificaciones automáticas a los clientes ante eventos
relevantes de su cuenta de puntos.

### 2. PRECONDICIONES
1. El cliente posee datos de contacto válidos registrados (CU-01/CU-03).

### 3. FLUJO PRINCIPAL (Camino Feliz - Proceso interno)
> Nota: este CU no expone un endpoint HTTP público; se ejecuta como *listener* de
> eventos internos (`NotificationDispatcher`), disparado por otros casos de uso.
1. El Sistema (Capa de Negocio) detecta un evento que requiere notificación,
   publicado por el caso de uso de origen (CU-07, CU-10, CU-11 o CU-21).
2. El Sistema genera el contenido de la notificación correspondiente al evento,
   respetando la antelación mínima configurable para vencimientos próximos
   (**RN-22**).
3. El Sistema envía la notificación al cliente por el canal configurado (email,
   push, SMS).
4. La **Capa de Persistencia** registra el envío de la notificación en
   `Notificaciones`, disponible para auditoría (**RN-13**).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Error en el envío (Resultado: Reintento):**
  1. El sistema detecta que en el Paso 3 el Sistema no logra enviar la notificación (ej. proveedor de
     email/SMS no disponible).
  2. El Sistema registra el error y reintenta el envío según la política de
     reintentos configurada (backoff exponencial, máximo N intentos).
  3. Si se agotan los reintentos, el Sistema marca la notificación con estado
     "Fallida" en `Notificaciones` (queda igualmente registrada para auditoría,
     **RN-13**). Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El canal de envío puede ser email, push o SMS según la preferencia y los datos
   de contacto disponibles del cliente; en todos los casos el registro y el
   resultado (enviado/fallido) siguen el mismo esquema.

### 6. POSTCONDICIONES
1. El cliente recibe la notificación correspondiente al evento (o se agotan los
   reintentos y queda marcada como fallida).
2. El envío (exitoso o fallido) queda registrado en `Notificaciones` (**RN-13**).

---

## Anexo: matrices de referencia

### Resultado del proceso (no aplica tabla de códigos HTTP — proceso interno)

| Resultado | Contexto de Aplicación en el Caso de Uso |
| --- | --- |
| `Enviada` | Notificación entregada correctamente por el canal configurado. |
| `Fallida` | Se agotaron los reintentos de envío; queda registrada para revisión. |

### Matriz de trazabilidad CU-23 → Test

| Paso del CU | Excepción / Resultado | Test unitario (BusinessLogic) | Test integración (Listener / Persistencia) |
| --- | --- | --- | --- |
| Flujo principal | `Enviada` | `EnviarNotificacionAsync_WithValidEvent_SendsAndLogsNotification` | `NotificationDispatcher_OnCanjeEvent_SendsAndPersistsNotification` |
| 3a. Error en el envío | `Fallida` (tras reintentos) | `EnviarNotificacionAsync_WhenProviderFailsRepeatedly_MarksAsFallidaAfterRetries` | `NotificationDispatcher_WhenProviderUnavailable_RetriesAndMarksFallida` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
