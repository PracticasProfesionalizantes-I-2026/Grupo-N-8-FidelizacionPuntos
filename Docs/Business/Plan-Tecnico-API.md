# Plan Técnico — API Fidelix en Arquitectura N-Tier (.NET)

> Nació como el Paso 1 (Planificación) del prompt de arquitectura de la
> cátedra: la estructura base de la API y las **3 entidades principales**.
> Sobre esa base se implementaron los **26 casos de uso**; este documento
> describe el estado actual. El catálogo completo de endpoints, con roles y
> códigos de respuesta, está en el `README.md` de la raíz del repositorio.

## 1. Alcance

Tres entidades principales, con la misma relación que el ejemplo de la
cátedra (Socio / Libro / Préstamo):

| Ejemplo cátedra | Fidelix | Rol |
| --- | --- | --- |
| Socio | **Cliente** | Quien participa del programa de puntos |
| Libro | **Beneficio** | Lo que se ofrece a cambio de puntos |
| Préstamo | **Movimiento** | Entidad hija que relaciona a las otras dos |

Los casos de uso modelo, uno de cada tipo, fijaron los patrones que siguen
todos los demás:

- **CU-01 Registrarse** — alta con reglas de negocio.
- **CU-04 Consultar saldo** — consulta calculada sobre movimientos.
- **CU-17 ABM de beneficio** — alta, modificación y baja lógica.

**Estado actual:** 10 entidades (sección 2), los 26 casos de uso (CU-01 a
CU-26), autenticación JWT con tres roles (Cliente, Empleado, Admin, RN-11),
auditoría automática de operaciones (RN-13), procesos diarios en segundo
plano (vencimiento de puntos CU-22 y bono de cumpleaños CU-26) y los niveles
de complejidad N1 (Gold x1.5) y N2 (puntos pendientes 15 días).

## 2. Entidades

Todas heredan de `EntidadBase` (`Id: int` autoincremental, lo genera la base de datos al
insertar; antes era un `Guid` y se cambió por observación del profesor: ids cortos y legibles).

**Núcleo (las 3 entidades del plan inicial):**

**Cliente**: `Nombre`, `Documento` (único), `Email` (único), `PasswordHash?`
(nulo si lo dio de alta el empleado y todavía no definió contraseña, CU-12),
`Telefono?`, `FechaNacimiento?`, `FechaRegistro`, `Activo`, `Categoria` (enum
`CategoriaCliente`: 1 = Estandar, 2 = Gold — N1), `IntentosFallidos` y
`BloqueadoHasta?` (bloqueo temporal, RN-03), `Movimientos`.

**Beneficio**: `Nombre` (único), `Descripcion?`, `CostoPuntos`,
`Categoria` (enum `CategoriaBeneficio`: 1 = Descuento, 2 = Producto gratis,
3 = Experiencia, 4 = Otro; numerado desde 1 para que el 0 no sea una categoría
válida), `Activo`, `Canjes`.

**Movimiento**: `ClienteId` (FK), `Tipo` (enum `TipoMovimiento`: Acumulacion,
Canje, BonoCumpleanos, Vencimiento), `Puntos`, `PuntosDisponibles` (lo que
queda del lote para el FIFO, RN-09), `Fecha`, `FechaVencimiento?`,
`FechaDisponible?` (fin del pendiente de 15 días, N2), `BeneficioId?` (FK,
solo en canjes), `EstadoCanje?` (Pendiente / Entregado, RN-10), `EmpleadoId?`
(quién registró la operación, RF-28), `Detalle?`.

**Agregadas al implementar el resto de los casos de uso:**

**Producto** (CU-18): `Nombre` (único), `Descripcion?`, `Precio` (RN-25),
`Activo`. La compra (CU-10) calcula el monto con estos precios.

**ReglaAcumulacion** (CU-19): `PuntosPorMonto`, `VigenciaDesde` (única),
`DiasVigenciaPuntos`, `FechaCreacion`, `FechaModificacion?`. Se versiona:
rige la de `VigenciaDesde` más reciente que ya empezó (RN-12).

**Empleado** (CU-16): `Nombre`, `Documento` (único), `Email` (único),
`PasswordHash`, `Activo`, `FechaAlta`.

**Admin** (CU-14): `Nombre`, `Email` (único), `PasswordHash`.

**Auditoria** (CU-21, RN-13/RN-20): `Fecha` (indexada), `TipoOperacion`,
`ActorId?`, `ActorRol`, `EntidadId?`, `Detalle`. Solo se crea y se consulta,
nunca se modifica.

**CodigoRecuperacion** (CU-25, RN-27): `Email`, `Rol`, `CodigoHash`,
`FechaExpiracion`, `Usado`.

Relaciones: `Movimiento → Cliente` y `Movimiento → Beneficio`, ambas con
`DeleteBehavior.Restrict` (las bajas son lógicas y nunca borran historial).

**Decisión: el saldo no es una columna.** Se calcula sumando
`PuntosDisponibles` de los lotes no vencidos (RN-05). Así no puede quedar
desincronizado con los vencimientos ni con los canjes.

## 3. Endpoints por capa

Cada controller delega en un service y cada service accede a los datos solo
por interfaces de repositorio. El detalle de cada endpoint (método, ruta, rol,
CU y respuestas) está en el catálogo del `README.md` de la raíz; esta tabla muestra
qué capas atraviesa cada grupo.

| Controller (rutas) | CU | Service | Repositorios |
| --- | --- | --- | --- |
| `AuthController` (`/api/auth/login`) | CU-02 | `AuthService` (+ `ITokenService`) | `IClienteRepository` |
| `AuthPersonalController` (`/api/auth/empleados/login`, `/admin/login`, `/recuperar-contrasena`) | CU-09, CU-14, CU-25 | `AuthPersonalService`, `RecuperacionService` (+ `ITokenService`, `IEmailSender`) | `IEmpleadoRepository`, `IAdminRepository`, `IClienteRepository`, `ICodigoRecuperacionRepository` |
| `ClientesController` (`/api/clientes/...`) | CU-01, CU-03, CU-04, CU-05, CU-08, CU-12, CU-13, búsqueda por DNI | `ClienteService` | `IClienteRepository`, `IMovimientoRepository` |
| `BeneficiosController` (`/api/beneficios`) | CU-06 | `BeneficioService` | `IBeneficioRepository` |
| `ProductosController` (`/api/productos`) | CU-10 | `ProductoService` | `IProductoRepository` |
| `MovimientosController` (`/api/movimientos/acumulaciones/{clienteId}`) | CU-10 | `MovimientoService` | `IClienteRepository`, `IProductoRepository`, `IReglaAcumulacionRepository`, `IMovimientoRepository` |
| `CanjesController` (`/api/canjes/...`) | CU-07, CU-11, CU-23 | `CanjeService` (+ `LotePuntosService`, FIFO) | `IClienteRepository`, `IBeneficioRepository`, `IMovimientoRepository` |
| `AdminClientesController` (`/api/admin/clientes`) | CU-15 | `ClienteAdminService` | `IClienteRepository` |
| `AdminEmpleadosController` (`/api/admin/empleados`) | CU-16 | `EmpleadoAdminService` | `IEmpleadoRepository` |
| `AdminBeneficiosController` (`/api/admin/beneficios`) | CU-17 | `BeneficioService` | `IBeneficioRepository` |
| `AdminProductosController` (`/api/admin/productos`) | CU-18 | `ProductoService` | `IProductoRepository` |
| `AdminReglasAcumulacionController` (`/api/admin/reglas-acumulacion`) | CU-19 | `ReglaAcumulacionService` | `IReglaAcumulacionRepository` |
| `AdminMovimientosController` (`/api/admin/movimientos`) | CU-20 | `MovimientoService` | `IMovimientoRepository` |
| `AdminAuditoriaController` (`/api/admin/auditoria`) | CU-21 | `AuditoriaService` | `IAuditoriaRepository` |
| `AdminProcesosController` (`/api/admin/procesos/...`) y `ProcesoDiarioJob` | CU-22, CU-26 | `PuntosService` (+ `IAuditoriaService`, `IEmailSender`) | `IClienteRepository`, `IMovimientoRepository`, `IReglaAcumulacionRepository` |
| `AdminReportesController` (`/api/admin/reportes`) | CU-24 (y comparación de CU-20) | `ReporteService` (+ `ReporteCsvWriter` en la capa API) | `IMovimientoRepository`, `IAuditoriaRepository` |

**Transversales:**

* **Autenticación:** JWT con tres roles (`Cliente`, `Empleado`, `Admin`).
  Cada controller o acción declara `[Authorize(Roles = ...)]`; sin token
  responde 401 y con un rol sin permiso 403 (RN-11). En los endpoints `/me`
  el id del cliente sale del token, nunca del JSON.
* **Auditoría:** `AuditoriaFilter` registra con `AuditoriaService` cada
  POST/PUT/PATCH/DELETE exitoso (operación, actor, rol y entidad afectada).

## 4. Reglas de negocio y excepciones tipadas

| Regla | Dónde | Excepción | HTTP |
| --- | --- | --- | --- |
| RN-01: documento y email de cliente únicos | `ClienteService` | `ClienteDuplicadoException` | 409 |
| RN-02: contraseña de al menos 8 caracteres (`PoliticaPassword`, una sola regla para todos los canales) | `ClienteService` (registro y perfil), `ClienteAdminService`, `EmpleadoAdminService`, `RecuperacionService` | `PasswordInvalidaException` | 400 |
| Fecha de nacimiento: no futura y edad entre 18 y 120 años (`PoliticaFechaNacimiento`) | `ClienteService` (registro, perfil y punto de venta) y `ClienteAdminService` (alta y modificación) | `FechaNacimientoInvalidaException` | 400 |
| Cliente inexistente (por id: saldo, compra, canje presencial, canjes de un cliente; o por DNI en la búsqueda) | `ClienteService` / `MovimientoService` / `CanjeService` | `ClienteNotFoundException` | 404 |
| CU-04 4a: falla al leer los movimientos | `ClienteService` | `PersistenceException` | 500 |
| RN-05: los lotes vencidos no suman al saldo | `MovimientoRepository` (filtro) | — | — |
| RN-16: costo en puntos mayor a cero y hasta 999.999 (6 dígitos enteros) | `BeneficioService` | `CostoInvalidoException` | 400 |
| RN-23: nombre de beneficio único | `BeneficioService` | `BeneficioDuplicadoException` | 409 |
| Beneficio inexistente | `BeneficioService` | `BeneficioNotFoundException` | 404 |
| RN-17: la desactivación es lógica | `BeneficioService` | — | — |
| RN-03: bloqueo temporal por intentos fallidos | `AuthService` | `CuentaBloqueadaException` | 403 |
| Login con credenciales incorrectas (CU-02, CU-09, CU-14) | `AuthService` / `AuthPersonalService` | `CredencialesInvalidasException` | 401 |
| RN-14/RN-15: cuenta inactiva (login, acumulación, canje) | `AuthService` / `AuthPersonalService` / `MovimientoService` / `CanjeService` | `CuentaInactivaException` | 403 en login, 409 en operaciones |
| RN-04: el documento no se modifica | `ClienteService` | `DocumentoNoModificableException` | 400 |
| RN-01: email de otro cliente al editar el perfil | `ClienteService` | `EmailDuplicadoException` | 409 |
| RN-01: empleado duplicado | `EmpleadoAdminService` | `EmpleadoDuplicadoException` | 409 |
| Empleado inexistente | `EmpleadoAdminService` | `EmpleadoNotFoundException` | 404 |
| RN-24: nombre de producto único | `ProductoService` | `ProductoDuplicadoException` | 409 |
| RN-25: precio mayor a cero, hasta 999.999,99 y con 2 decimales como máximo | `ProductoService` | `PrecioInvalidoException` | 400 |
| Producto inexistente | `ProductoService` | `ProductoNotFoundException` | 404 |
| RN-18: regla de acumulación inválida o superpuesta | `ReglaAcumulacionService` | `ReglaAcumulacionInvalidaException` | 400 |
| Regla de acumulación inexistente | `ReglaAcumulacionService` | `ReglaAcumulacionNotFoundException` | 404 |
| RN-12: sin regla vigente al acumular | `MovimientoService` | `ReglaAcumulacionNoVigenteException` | 409 |
| DNI con formato inválido en la búsqueda del punto de venta (`GET /api/clientes?documento=`; antes CU-10 3b) | `ClienteService` | `DocumentoInvalidoException` | 400 |
| RN-26: ítem con producto inexistente o inactivo | `MovimientoService` | `ItemCompraInvalidoException` | 400 |
| RN-17: beneficio inactivo al canjear | `CanjeService` | `BeneficioInactivoException` | 409 |
| RN-08: saldo insuficiente | `CanjeService` | `SaldoInsuficienteException` | 409 |
| RN-10: canje inexistente / ya entregado | `CanjeService` | `CanjeNotFoundException` / `CanjeYaEntregadoException` | 404 / 409 |
| Registro de auditoría inexistente | `AuditoriaService` | `AuditoriaNotFoundException` | 404 |
| RN-27: código de recuperación inválido o vencido | `RecuperacionService` | `CodigoRecuperacionInvalidoException` | 400 |
| Sin token o token inválido / rol sin permiso (RN-11) | Middleware JWT + `[Authorize(Roles)]` | — | 401 / 403 |

Errores de sintaxis del JSON o DataAnnotations: 400 automático de
`[ApiController]`, antes de llegar al service (formato estándar `ProblemDetails`).

**Formato de las respuestas:** los errores de negocio (las excepciones de esta
tabla) responden un `ErrorResponseDTO` `{ "mensaje": "..." }`; los listados
responden `ListadoResponseDTO` `{ "mensaje", "items" }`. Un id que no es un número entero
en una ruta con restricción `{id:int}` no coincide con ningún endpoint: 404.

**Identificación del cliente:** las operaciones sobre un cliente reciben su id
(entero autoincremental, clave primaria y foránea) como parámetro de ruta, por ejemplo
`POST /api/movimientos/acumulaciones/{clienteId}`. El DNI solo se usa para
buscarlo (`GET /api/clientes?documento=`).

## 5. Fases de desarrollo

1. **Fase 1**: `Shared/` (DTOs, excepciones, enums, `PasswordHasher`) +
   `DataAccess/` (entidades, DbContext, repositorios, migración inicial,
   `DbInitializer`).
2. **Fase 2**: `BusinessLogic/` (interfaces y services) + tests unitarios
   (xUnit + Moq).
3. **Fase 3**: `API/` (controllers) + tests de integración
   (`WebApplicationFactory`) + colección Bruno.
4. **Paso 3**: `README.md` y `AGENTS.md`.
5. **Resto de los casos de uso** (CU-02 a CU-26): login con JWT y roles,
   ABM de clientes, empleados y productos, reglas de acumulación, acumulación
   y canjes (N1, N2, FIFO), auditoría, procesos diarios, reportes y
   recuperación de contraseña, cada uno con sus tests, requests de Bruno y su
   especificación actualizada en `Docs/Use cases/`.
6. **Ajustes por observaciones del profesor**: el cliente se identifica por
   id y no por DNI, el id viaja como parámetro de ruta, y todas las
   respuestas son DTOs (`ErrorResponseDTO`, `ListadoResponseDTO`).
7. **Ids enteros autoincrementales** (observación del profesor): todas las
   entidades pasan de `Guid` a `int` generado por la base. Se reemplazaron las
   migraciones anteriores por una única `InitialCreate` (SQLite no permite
   cambiar el tipo de una clave primaria existente) y el seed usa ids fijos
   (cliente demo 1, cliente Gold 3, admin 1, empleado 1). Los ids se repiten
   entre tablas (el cliente 1 y el empleado 1 son personas distintas): el
   token y la auditoría los distinguen por el rol.
