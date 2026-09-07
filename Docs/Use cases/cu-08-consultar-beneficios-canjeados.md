# Caso de Uso: Consultar Beneficios Canjeados

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Regla de negocio RN-10 (estado del canje visible) **a implementar**; cada caso
> borde debe contar con su test unitario e integración (ver matriz de
> trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-08 |
| **Nombre** | Consultar beneficios canjeados |
| **Actor Principal** | Cliente |
| **Alcance / Nivel** | Sistema; subfunción |
| **Stakeholders e intereses** | Cliente → saber qué canjeó y en qué estado está; Empleado → confirmar en el local qué beneficios tiene pendientes de entrega el cliente |
| **Disparador (Trigger)** | El cliente solicita ver sus beneficios canjeados |
| **Prioridad / Frecuencia** | Media; uso frecuente |
| **Reglas de negocio relacionadas** | RN-10 (estado del canje visible: pendiente/entregado) |

---

### 1. BREVE DESCRIPCIÓN
Permite al cliente visualizar el listado de beneficios que canjeó previamente, con
su fecha y estado.

### 2. PRECONDICIONES
1. El actor debe poseer un estado de autenticación activo (Token JWT válido,
   CU-02).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `GET /api/clientes/me/canjes`.
2. La **Capa de Presentación** (`ClientesController.ConsultarCanjes`) identifica al
   cliente a partir del token JWT.
3. La **Capa de Negocio** (`ClienteService.ConsultarCanjesAsync`) recupera los
   canjes del cliente, incluyendo su estado (**RN-10**).
4. La **Capa de Persistencia** recupera los registros (`Canjes`) del cliente.
5. El Sistema devuelve un código **200 OK** con el listado de beneficios
   canjeados, su fecha y estado.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **4a. Sin canjes registrados (HTTP 200 OK):**
  1. El sistema detecta que en el Paso 4 el Sistema no encuentra canjes realizados por el cliente.
  2. El Sistema no interrumpe el flujo: no es un error, es un resultado vacío
     válido.
  3. El Sistema devuelve un código **200 OK** con una lista vacía y el mensaje:
     "Aún no realizaste ningún canje". Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: consulta de lectura simple, sin variantes de mecanismo relevantes._

### 6. POSTCONDICIONES
1. El cliente visualiza el historial de sus canjes, sin que se modifique ningún
   dato persistente.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al recuperar el listado (con o sin resultados). |

### Matriz de trazabilidad CU-08 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `ConsultarCanjesAsync_ReturnsCanjesWithStatus` | `ConsultarCanjes_WithValidToken_Returns200OK` |
| 4a. Sin canjes registrados | `200 OK` (lista vacía) | `ConsultarCanjesAsync_WithNoCanjes_ReturnsEmptyList` | `ConsultarCanjes_WithNoCanjes_Returns200OKWithEmptyList` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
