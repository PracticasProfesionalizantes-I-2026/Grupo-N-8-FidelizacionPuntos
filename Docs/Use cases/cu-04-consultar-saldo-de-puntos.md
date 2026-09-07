# Caso de Uso: Consultar Saldo de Puntos

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Regla de negocio RN-05 (saldo neto, descontando vencidos) **a implementar**;
> cada caso borde debe contar con su test unitario e integración (ver matriz de
> trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-04 |
| **Nombre** | Consultar saldo de puntos |
| **Actor Principal** | Cliente |
| **Alcance / Nivel** | Sistema; subfunción |
| **Stakeholders e intereses** | Cliente → conocer cuántos puntos puede canjear; Negocio → mostrar información confiable que impulse el canje |
| **Disparador (Trigger)** | El cliente solicita consultar su saldo de puntos |
| **Prioridad / Frecuencia** | Alta; uso muy frecuente |
| **Reglas de negocio relacionadas** | RN-05 (saldo neto de puntos vigentes) |

---

### 1. BREVE DESCRIPCIÓN
Permite al cliente visualizar la cantidad de puntos vigentes disponibles en su
cuenta, ya descontados los puntos vencidos.

### 2. PRECONDICIONES
1. El actor debe poseer un estado de autenticación activo (Token JWT válido,
   CU-02).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `GET /api/clientes/me/saldo`.
2. La **Capa de Presentación** (`ClientesController.ConsultarSaldo`) identifica al
   cliente a partir del token JWT.
3. La **Capa de Negocio** (`ClienteService.ConsultarSaldoAsync`) calcula el saldo
   vigente sumando los lotes de puntos no vencidos, aplicando **RN-05**.
4. La **Capa de Persistencia** recupera los lotes de puntos (`Movimientos`) del
   cliente.
5. El Sistema devuelve un código **200 OK** con el saldo de puntos disponible.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **4a. Error al recuperar el saldo (HTTP 500 Internal Server Error):**
  1. El sistema detecta que en el Paso 4 la Capa de Persistencia no logra recuperar la información
     (ej. falla de conexión).
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error** con el mensaje:
     "No se pudo obtener el saldo en este momento". Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: consulta de lectura simple, sin variantes de mecanismo relevantes._

### 6. POSTCONDICIONES
1. El cliente visualiza su saldo de puntos vigente y actualizado.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al recuperar y devolver el saldo vigente. |
| `500` | Internal Server Error | Error técnico no controlado al recuperar el saldo. |

### Matriz de trazabilidad CU-04 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `ConsultarSaldoAsync_WithExpiredAndActiveLotes_ReturnsNetBalance` | `ConsultarSaldo_WithValidToken_Returns200OK` |
| 4a. Error al recuperar el saldo | `500 Internal Server Error` | `ConsultarSaldoAsync_WhenRepositoryFails_ThrowsPersistenceException` | `ConsultarSaldo_WhenPersistenceFails_Returns500InternalServerError` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
