# Caso de Uso: Consultar Beneficios

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Regla de negocio RN-07 (solo beneficios activos) **a implementar**; cada caso
> borde debe contar con su test unitario e integración (ver matriz de
> trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-06 |
| **Nombre** | Consultar beneficios |
| **Actor Principal** | Cliente |
| **Alcance / Nivel** | Sistema; subfunción |
| **Stakeholders e intereses** | Cliente → conocer qué puede canjear con sus puntos; Negocio → exhibir un catálogo vigente y atractivo |
| **Disparador (Trigger)** | El cliente solicita ver los beneficios disponibles |
| **Prioridad / Frecuencia** | Alta; uso muy frecuente |
| **Reglas de negocio relacionadas** | RN-07 (solo beneficios activos son visibles) |

---

### 1. BREVE DESCRIPCIÓN
Permite al cliente visualizar el catálogo de beneficios activos disponibles para
canje, junto con su costo en puntos.

### 2. PRECONDICIONES
1. El actor debe poseer un estado de autenticación activo (Token JWT válido,
   CU-02).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `GET /api/beneficios`.
2. La **Capa de Presentación** (`BeneficiosController.Listar`) recibe la petición.
3. La **Capa de Negocio** (`BeneficioService.ListarBeneficiosAsync`) filtra
   únicamente los beneficios con estado activo, cumpliendo **RN-07**.
4. La **Capa de Persistencia** recupera los beneficios (`Beneficios`) activos.
5. El Sistema devuelve un código **200 OK** con el listado de beneficios y su costo
   en puntos.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **4a. No hay beneficios disponibles (HTTP 200 OK):**
  1. Si en el Paso 4 el Sistema no encuentra beneficios activos.
  2. El Sistema no interrumpe el flujo: no es un error, es un resultado vacío
     válido.
  3. El Sistema devuelve un código **200 OK** con una lista vacía y el mensaje: "No
     hay beneficios disponibles". Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No aplica: consulta de lectura simple, sin variantes de mecanismo relevantes._

### 6. POSTCONDICIONES
1. El cliente visualiza el catálogo de beneficios vigente, sin que se modifique
   ningún dato persistente.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Éxito al recuperar el catálogo (con o sin resultados). |

### Matriz de trazabilidad CU-06 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `ListarBeneficiosAsync_ReturnsOnlyActiveBeneficios` | `ListarBeneficios_Returns200OKWithActiveList` |
| 4a. Sin beneficios disponibles | `200 OK` (lista vacía) | `ListarBeneficiosAsync_WithNoActiveBeneficios_ReturnsEmptyList` | `ListarBeneficios_WithNoActiveBeneficios_Returns200OKWithEmptyList` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
