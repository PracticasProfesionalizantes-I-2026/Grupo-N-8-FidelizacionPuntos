# Plan Técnico — API Fidelix en Arquitectura N-Tier (.NET)

> Paso 1 (Planificación) del prompt maestro de la adenda de arquitectura en
> capas. Cubre los 26 casos de uso vigentes (CU-01 a CU-26). **No incluye
> código** — es la base para aprobar antes de arrancar la Fase 1 de desarrollo
> (`Shared/` + `DataAccess/`).
>
> Proyecto: `src/` (raíz del repo Fidelix), estructura `API/`, `BusinessLogic/`,
> `DataAccess/`, `Shared/`, `Migrations/`, `bruno/`.

---

## 1. Decisiones de diseño tomadas en este Paso 1

Vacíos detectados en el relevamiento de los 26 CU (los dos primeros resueltos
antes de cerrar el plan; los dos últimos, sobre la marcha al codificar la
Fase 1/2):

1. **Colisión de endpoint de login (CU-02/CU-09/CU-14):** las tres CU definen
   `POST /api/auth/login` sin distinguir el rol. Se resuelve con **un único
   endpoint** en `AuthController`: `AuthService.LoginAsync` prueba las
   credenciales en cascada **Cliente → Empleado → Admin** (por email +
   password) y emite un JWT con un claim de rol (`ClaimTypes.Role`) según cuál
   matcheo. Si ninguna cuenta coincide, se lanza `CredencialesInvalidasException`
   (401).
2. **Gap en CU-19 (Configurar Reglas de Acumulación):** solo definía `POST` y
   `PUT`, sin lectura, a diferencia del resto de los módulos admin. Se agregó
   `GET /api/admin/reglas-acumulacion` (listado) y `GET
   /api/admin/reglas-acumulacion/{id}` (detalle, con `ReglaAcumulacionNotFoundException`
   → 404) directamente en `cu-19-configurar-reglas-de-acumulacion.md`, para
   mantenerlo simétrico con `AdminClientesController`, `AdminEmpleadosController`,
   etc.
3. **Entidad `Auditoria` faltante:** al empezar a codificar la Fase 1 se detectó
   que CU-21 (Auditar Operaciones) y CU-24 (Generar Reportes) referencian una
   tabla `Auditoria` propia (con filtros `fechaDesde`, `fechaHasta`,
   `tipoOperacion`, `actorId`), distinta de `Movimientos`. La versión previa de
   este plan solo cubría trazabilidad vía `Movimiento.EmpleadoId`, lo cual no
   alcanza para auditar operaciones que no son movimientos de puntos (ABM de
   clientes/empleados/beneficios/productos, cambios de reglas de acumulación).
   Se agrega la entidad `Auditoria` (sección 2) como registro de solo inserción
   (RN-20) de todas las operaciones relevantes del sistema.
4. **RF-22 (lapso de vencimiento) sin CU propio:** al implementar CU-22/CU-26
   se detectó que RF-22 ("el admin aplica lapso de vencimiento a los puntos")
   no tiene ningún caso de uso que diga dónde se configura ese lapso — ni
   CU-19 ni CU-22 lo definían. Se agregó `DiasVigenciaPuntos` a
   `ReglaAcumulacion` (y a CU-19) en vez de crear un CU y una entidad nuevos,
   ya que es un dato que el admin configura junto con `PuntosPorMonto`, con
   la misma frecuencia de uso.
5. **Campos de bloqueo por intentos fallidos (RN-03):** `Cliente`, `Empleado`
   y `Admin` no tenían dónde guardar el contador de intentos fallidos ni el
   bloqueo temporal que exige RN-03 (CU-02/CU-09/CU-14). Se extrajo una base
   común `CuentaConCredenciales` (Email, PasswordHash, Activo,
   IntentosFallidos, BloqueadoHasta) para no triplicar estos 4 campos en las
   tres entidades.
6. **`PuntosDisponibles` por lote (RN-05, RN-09):** para poder calcular el
   saldo disponible y aplicar FIFO sin recorrer todo el historial de canjes,
   cada lote de acumulación/bono necesita su propio saldo restante mutable,
   además del monto original (histórico) en `Puntos`. Se agregó
   `Movimiento.PuntosDisponibles`.

---

## 2. Modelo de entidades (`DataAccess/Entities`)

Todas las entidades usan `Guid` como Id, asignado en el repositorio
(`CreateAsync`), y `Activo: bool` para baja lógica donde corresponde (RN-14,
RN-15, RN-17, RN-26).

| Entidad | Campos propios | Relaciones | Reglas de negocio asociadas |
| --- | --- | --- | --- |
| **Cliente** | Nombre, Documento (inmutable), Email (único), PasswordHash, Telefono, FechaNacimiento, Activo, FechaRegistro | 1‑N `Movimiento` (Restrict) | RN-01, RN-04, RN-14, RN-29 |
| **Empleado** | Nombre, Documento, Email (único, login), PasswordHash, Activo, FechaAlta | 1‑N `Movimiento` (Restrict, nullable) | RN-15 |
| **Admin** | Nombre, Email (único), PasswordHash, Activo | — (destino de códigos de recuperación de Empleado, RN-28) | — |
| **Producto** | Nombre (único), Descripcion, Precio (>0), Activo | — | RN-24, RN-25, RN-26 |
| **Beneficio** | Nombre (único), Descripcion, CostoPuntos (>0), Categoria (enum), Activo | 1‑N `Movimiento` (Restrict, nullable, en canjes) | RN-16, RN-17, RN-23 |
| **ReglaAcumulacion** | PuntosPorMonto, VigenciaDesde, VigenciaHasta (nullable), DiasVigenciaPuntos (RF-22), Activa | — | RN-18, RN-21, RF-22 |
| **Movimiento** | Tipo (enum: `Acumulacion`, `Canje`, `BonoCumpleanos`, `Vencimiento`), Puntos (monto original, histórico), PuntosDisponibles (saldo restante del lote, mutable), Fecha, FechaVencimiento (nullable, lotes de acumulación), Detalle | N‑1 `Cliente` (Restrict); N‑1 `Empleado` (Restrict, nullable — null en procesos de Sistema: CU-22/CU-26); N‑1 `Beneficio` (Restrict, nullable, solo en canjes) | RN-05, RN-06, RN-09, RN-12, RN-13, RN-21 |
| **CodigoRecuperacion** *(nueva, CU-25)* | Codigo (hasheado), FechaCreacion, FechaExpiracion, Usado | N‑1 `Cliente` / N‑1 `Empleado` / N‑1 `Admin` (nullable, exactamente uno no-nulo según el rol que solicitó) | RN-27, RN-28 |
| **Auditoria** *(nueva, CU-21/CU-24)* | Fecha, TipoOperacion (string), ActorTipo (enum: Empleado/Admin/Sistema), ActorId (nullable), EntidadAfectada, EntidadId (nullable), Detalle | Sin FK estrictas (referencias lógicas a ActorId/EntidadId, no navegación EF, para no acoplar el log a la entidad auditada) | RN-13, RN-19, RN-20 (solo inserción; sin `UpdateAsync`/`DeleteAsync` en el repositorio) |

**Nota sobre `Movimiento`:** se modela como una única tabla con `Tipo` en vez
de tablas separadas por tipo de movimiento (acumulación/canje/bono/vencimiento),
porque todas comparten el mismo ciclo de vida (saldo, vencimiento, auditoría,
FIFO) y separar tablas obligaría a duplicar la lógica de saldo y de RN-13 en
cuatro lugares. La columna `EmpleadoId` (nullable) es el campo agregado para
**RF-28** (atribución de empleado en movimientos): queda `null` únicamente en
los procesos batch de Sistema (CU-22, CU-26); en CU-10 y CU-11 es obligatorio.

---

## 3. Excepciones tipadas (`Shared/Exceptions`, un archivo por clase)

| Excepción | HTTP | Se lanza cuando… |
| --- | --- | --- |
| `ValidationException` | 400 | Falla de validación genérica (campos requeridos/formato) no cubierta por una excepción más específica. |
| `PasswordInvalidaException` | 400 | La contraseña no cumple la política mínima (RN-02). |
| `CostoInvalidoException` | 400 | `CostoPuntos` de un beneficio ≤ 0 (RN-16). |
| `PrecioInvalidoException` | 400 | `Precio` de un producto ≤ 0 (RN-25). |
| `ReglaAcumulacionInvalidaException` | 400 | Configuración inválida o solapamiento con una regla activa (RN-18). |
| `CodigoRecuperacionInvalidoException` | 400 | Código de recuperación inexistente, vencido o ya usado (RN-27). |
| `CredencialesInvalidasException` | 401 | Login: ninguna cuenta (Cliente/Empleado/Admin) matchea email+password. |
| `CuentaBloqueadaException` | 403 | Login bloqueado por intentos fallidos (RN-03). |
| `CuentaInactivaException` | 403 | Actor autenticado pero dado de baja intenta operar (RN-14, RN-15). |
| `ClienteNotFoundException` | 404 | No existe el cliente solicitado (por id o documento). |
| `EmpleadoNotFoundException` | 404 | No existe el empleado solicitado. |
| `BeneficioNotFoundException` | 404 | No existe el beneficio solicitado. |
| `ProductoNotFoundException` | 404 | No existe el producto solicitado. |
| `ReglaAcumulacionNotFoundException` | 404 | No existe la regla de acumulación solicitada *(nueva, cierre del gap CU-19)*. |
| `ClienteDuplicadoException` | 409 | Documento o email de cliente ya registrado (RN-01, RN-04). |
| `EmpleadoDuplicadoException` | 409 | Documento o email de empleado ya registrado. |
| `EmailDuplicadoException` | 409 | Email ya usado por otro cliente al modificar el perfil (RN-01). |
| `BeneficioDuplicadoException` | 409 | Nombre de beneficio ya existente (RN-23). |
| `ProductoDuplicadoException` | 409 | Nombre de producto ya existente (RN-24). |
| `BeneficioInactivoException` | 409 | Intento de canje de un beneficio desactivado (RN-17). |
| `SaldoInsuficienteException` | 409 | Saldo de puntos insuficiente para el canje (RN-08). |
| `PersistenceException` | 500 | Falla no controlada de la Capa de Persistencia. |

22 excepciones en total.

---

## 4. Endpoints por controlador (`API/Controllers`)

| Controller | Endpoint | CU | Éxito | Excepciones posibles |
| --- | --- | --- | --- | --- |
| **AuthController** | `POST /api/auth/login` | CU-02, CU-09, CU-14 | 200 | `CredencialesInvalidasException`, `CuentaBloqueadaException`, `CuentaInactivaException` |
| | `POST /api/auth/recuperar-contrasena` | CU-25 | 200 (siempre, no revela existencia) | — |
| | `POST /api/auth/recuperar-contrasena/confirmar` | CU-25 | 200 | `CodigoRecuperacionInvalidoException`, `PasswordInvalidaException` |
| **ClientesController** | `POST /api/clientes/registro` | CU-01 | 201 | `ClienteDuplicadoException`, `PasswordInvalidaException` |
| | `GET /api/clientes/me` | CU-03 | 200 | — |
| | `PUT /api/clientes/me` | CU-03 | 200 | `ValidationException`, `EmailDuplicadoException` |
| | `DELETE /api/clientes/me` | CU-03 | 204 | — |
| | `GET /api/clientes/me/saldo` | CU-04 | 200 | `PersistenceException` |
| | `GET /api/clientes/me/movimientos` | CU-05 | 200 | — |
| | `GET /api/clientes/me/canjes` | CU-08 | 200 | — |
| | `POST /api/clientes` *(desde POS, empleado)* | CU-12 | 201 | `ClienteDuplicadoException` |
| | `GET /api/clientes/{documento}/canjes` *(empleado)* | CU-13 | 200 | `ClienteNotFoundException` |
| **BeneficiosController** | `GET /api/beneficios` | CU-06 | 200 | — |
| **CanjesController** | `POST /api/canjes` | CU-07 (cliente), CU-11 (empleado presencial) | 201 | `BeneficioNotFoundException`/`ClienteNotFoundException`, `BeneficioInactivoException`, `SaldoInsuficienteException`, `PersistenceException` |
| **MovimientosController** | `POST /api/movimientos/acumulaciones` | CU-10 | 201 | `ClienteNotFoundException`, `PersistenceException` |
| **AdminClientesController** | `POST` / `PUT {id}` / `DELETE {id}` `/api/admin/clientes` | CU-15 | 201/200/204 | `ClienteDuplicadoException`, `ClienteNotFoundException` |
| **AdminEmpleadosController** | `POST` / `PUT {id}` / `DELETE {id}` `/api/admin/empleados` | CU-16 | 201/200/204 | `EmpleadoDuplicadoException`, `EmpleadoNotFoundException` |
| **AdminBeneficiosController** | `POST` / `PUT {id}` / `PATCH {id}/desactivar` `/api/admin/beneficios` | CU-17 | 201/200/200 | `CostoInvalidoException`, `BeneficioDuplicadoException`, `BeneficioNotFoundException` |
| **AdminProductosController** | `POST` / `PUT {id}` / `PATCH {id}/desactivar` `/api/admin/productos` | CU-18 | 201/200/200 | `PrecioInvalidoException`, `ProductoDuplicadoException`, `ProductoNotFoundException` |
| **AdminReglasAcumulacionController** | `GET` / `GET {id}` / `POST` / `PUT {id}` `/api/admin/reglas-acumulacion` | CU-19 | 200/200/201/200 | `ReglaAcumulacionInvalidaException`, `ReglaAcumulacionNotFoundException` |
| **AdminMovimientosController** | `GET /api/admin/movimientos` | CU-20 | 200 | — |
| **AdminAuditoriaController** | `GET /api/admin/auditoria`, `GET /api/admin/auditoria/{id}` | CU-21 | 200 | — |
| **AdminReportesController** | `GET /api/admin/reportes` | CU-24 | 200 | — |

**Procesos batch sin endpoint HTTP** (`BusinessLogic/Jobs`):

| Job | CU | Servicio invocado | Notas |
| --- | --- | --- | --- |
| `VencimientoPuntosJob` | CU-22 | `PuntosService.AplicarVencimientoAsync` | Diario; resiliente a fallos por cliente. |
| `BonoCumpleanosJob` | CU-26 | `PuntosService.AplicarBonoCumpleanosAsync` | Diario; dedupe anual por cliente (RN-29). |
| *(lógica FIFO, no es un job)* | CU-23 | `MovimientoService`/`CanjeService` al ejecutar `POST /api/canjes` | Se aplica en el momento del canje, no en un proceso aparte. |

---

## 5. Estado de avance

1. **Fase 1 (completa):** `Shared/` (DTOs + 22 excepciones) + `DataAccess/`
   (9 entidades, `DbContext` con `DeleteBehavior.Restrict`, migraciones,
   `DbInitializer` con datos de prueba). `dotnet build` y `dotnet run` (crea
   la base y siembra los datos) verificados.
2. **Fase 2 (completa):** `BusinessLogic/` — 12 interfaces + servicios
   (`AuthService`, `ClienteService`, `ClienteAdminService`,
   `EmpleadoAdminService`, `ProductoService`, `BeneficioService`,
   `ReglaAcumulacionService`, `PuntosService`, `MovimientoService`,
   `CanjeService`, `AuditoriaService`, `ReporteService`) + proyecto
   `BusinessLogic.Tests` (xUnit + Moq), 53 tests cubriendo el flujo principal
   y las excepciones típicas de cada servicio. `dotnet test` verificado en
   verde.
3. **Fase 3 (completa):** `API/` — 13 controllers (uno por módulo del punto 4
   del plan), autorización por rol (`[Authorize(Roles = "...")]`) y manejo
   de excepciones por controller (try/catch explícito, sin middleware
   global genérico, según la adenda) + proyecto `API.IntegrationTests`
   (`WebApplicationFactory`, SQLite en memoria), 15 tests + colección Bruno
   en `bruno/` (53 requests, un endpoint por request con su caso de éxito y
   al menos un caso de error). Verificado end-to-end con `dotnet run` +
   `curl` (login de los 3 roles, acumulación → saldo → canje con
   descuento FIFO, autorización por rol) y con la colección Bruno completa
   vía `@usebruno/cli` (52/53 — el único "fallo" es un placeholder de
   código de recuperación documentado a propósito, ya que ese código solo
   existe en el log de la consola).

Cada fase se implementó y se validó (build/test) antes de pasar a la
siguiente, según la metodología de la adenda.
