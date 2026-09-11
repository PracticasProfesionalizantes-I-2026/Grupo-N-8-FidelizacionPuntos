# Caso de Uso: Aplicar Bono de Cumpleaños

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), adaptada a un caso de uso
> de **Sistema** (proceso batch diario, sin actor humano ni endpoint HTTP
> público).
> Regla de negocio RN-29 (bono de cumpleaños único por año) **a implementar**;
> cada caso borde debe contar con su test unitario e integración (ver matriz
> de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-26 |
| **Nombre** | Aplicar bono de cumpleaños |
| **Actor Principal** | Sistema |
| **Alcance / Nivel** | Sistema; subfunción (proceso batch) |
| **Stakeholders e intereses** | Cliente → recibir un beneficio espontáneo en su cumpleaños; Negocio → reforzar la fidelización con un gesto de reconocimiento |
| **Disparador (Trigger)** | Se ejecuta el proceso batch diario de acreditación de bonos |
| **Prioridad / Frecuencia** | Baja; se ejecuta una vez por día |
| **Reglas de negocio relacionadas** | RN-29 (bono de cumpleaños único por año, por cliente) |

---

### 1. BREVE DESCRIPCIÓN
Permite al sistema acreditar automáticamente un monto fijo de puntos a los
clientes activos cuya fecha de nacimiento coincide con el día en que se
ejecuta el proceso.

### 2. PRECONDICIONES
1. El cliente tiene registrada su fecha de nacimiento (`FechaNacimiento`).
2. El cliente se encuentra activo (**RN-14**).
3. El cliente no recibió el bono de cumpleaños en el año en curso (**RN-29**).

### 3. FLUJO PRINCIPAL (Camino Feliz - Proceso interno)
> Nota: este CU no expone un endpoint HTTP público; se ejecuta como un job
> programado (`BonoCumpleanosJob`), una vez por día.
1. El Sistema (Capa de Negocio, `PuntosService.AplicarBonoCumpleanosAsync`)
   identifica a los clientes activos cuya `FechaNacimiento` (día y mes)
   coincide con la fecha de ejecución.
2. Para cada cliente encontrado, el Sistema verifica que no haya recibido el
   bono en el año en curso (**RN-29**).
3. El Sistema acredita el monto fijo configurado (`BonoCumpleanosPuntos`) como
   un nuevo lote de puntos, con su propia fecha de vencimiento (misma lógica
   que cualquier acumulación, RN-05/RN-21).
4. La **Capa de Persistencia** registra el movimiento (`Movimientos`, tipo
   `BonoCumpleanos`) para fines de auditoría (**RN-13**) y actualiza el saldo
   disponible del cliente.
5. El Sistema continúa con el siguiente cliente hasta agotar la lista del día.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. El cliente ya recibió el bono este año (Resultado: Sin cambios):**
  1. El sistema detecta que en el Paso 2 ya existe un movimiento
     `BonoCumpleanos` para ese cliente en el año en curso, violando lo que
     impediría **RN-29**.
  2. El Sistema omite a ese cliente sin generar ningún movimiento ni error.
  3. El Sistema continúa con el siguiente cliente del lote. Fin del caso de
     uso para ese cliente.

* **4a. Error interno en la persistencia (Resultado: Reintento posterior):**
  1. El sistema detecta que en el Paso 4 la Capa de Persistencia no puede
     registrar el movimiento o actualizar el saldo de un cliente puntual.
  2. El Sistema registra el error como no controlado y continúa con el
     siguiente cliente, sin interrumpir el proceso batch completo.
  3. El cliente afectado queda pendiente para el reintento en la siguiente
     ejecución del job. Fin del caso de uso para ese cliente.

### 5. SUB-VARIACIONES (opcional)
_No aplica: el proceso se ejecuta por un único mecanismo (job programado)._

### 6. POSTCONDICIONES
1. Los clientes cumpleañeros del día quedan con un nuevo lote de puntos
   acreditado y su saldo actualizado.
2. El bono queda registrado en `Movimientos`, disponible para auditoría
   (**RN-13**), y no se vuelve a acreditar al mismo cliente en el mismo año
   (**RN-29**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

_No aplica: este caso de uso no expone endpoints HTTP (proceso interno de
Sistema)._

### Matriz de trazabilidad CU-26 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `Éxito` | `AplicarBonoCumpleanosAsync_WithMatchingClientes_AccruesPointsAndLogsAudit` | *(sin endpoint HTTP; cubierto por test de integración del job)* |
| 2a. Bono ya recibido este año | `Sin cambios` | `AplicarBonoCumpleanosAsync_WhenAlreadyGrantedThisYear_SkipsCliente` | — |
| 4a. Error interno de persistencia | `Sin cambios (reintento)` | `AplicarBonoCumpleanosAsync_WhenRepositoryFailsForOneCliente_ContinuesWithNext` | — |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
