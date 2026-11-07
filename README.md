# Fidelix — Sistema de fidelización de puntos

**Grupo N° 8** · Integrantes: Daniel Rodriguez

API RESTful del sistema de fidelización de puntos **Fidelix**, hecha en
.NET 10 con arquitectura en capas (N-Tier) sobre SQLite, siguiendo el prompt
de arquitectura de la cátedra.

Implementa los **26 casos de uso** de `Docs/Use cases/` (CU-01 a CU-26):
registro y login de los tres roles (cliente, empleado, admin) con JWT,
acumulación de puntos con reglas vigentes y categoría Gold (N1), puntos
pendientes 15 días (N2), canjes con descuento FIFO, vencimiento de puntos y
bono de cumpleaños automáticos, auditoría de operaciones, reportes (JSON y
CSV) y recuperación de contraseña.

La estructura base y los casos de uso modelo **CU-01** (alta con reglas de
negocio), **CU-04** (consulta calculada) y **CU-17** (ABM con baja lógica)
marcaron los patrones que siguen todos los demás.

## Documentación

| Recurso | Dónde |
| --- | --- |
| Documentación del proyecto y Lista y especificación de casos de uso | [Carpeta de Google Drive](https://drive.google.com/drive/folders/1xrh83MMljIAtshCEMXHefF7_iGJS0XPV?usp=sharing) |
| Prototipo Navegable (Datos Simulados | [Fidelix](https://practicasprofesionalizantes-i-2026.github.io/Grupo-N-8-FidelizacionPuntos/prototipo/) |
| Especificación de cada caso de uso (en el repo) | `Docs/Use cases/` (CU-01 a CU-26, con su matriz de trazabilidad a tests) |
| Reglas de negocio, requerimientos y niveles de complejidad | `Docs/Business/Documentacion-Fidelix.md` |
| Plan técnico: entidades y capas | `Docs/Business/Plan-Tecnico-API.md` |
| Colección de Bruno (cómo correrla) | `src/bruno/README.md` |
| Contexto para agentes de IA | `src/AGENTS.md` |

## Arquitectura

```
Docs/
├── Business/               Documento de negocio y plan técnico
├── Use cases/              Especificación de los 26 casos de uso
├── Mockups/                Capturas del prototipo
└── Example/                Guía y ejemplo de especificación de la cátedra
src/
├── API/                    Presentación
│   ├── Controllers/        16 controllers (cliente, empleado, admin y auth)
│   ├── Filters/            AuditoriaFilter: audita cada operación exitosa (RN-13)
│   ├── Jobs/               ProcesoDiarioJob: vencimiento de puntos (CU-22) y bono de cumpleaños (CU-26)
│   ├── Formatters/         ReporteCsvWriter: descarga de reportes en CSV (CU-24)
│   ├── Extensions/         ClaimsPrincipalExtensions: id del usuario desde el token
│   └── Program.cs          DI, JWT, Scalar
├── BusinessLogic/          Negocio
│   ├── Interfaces/         Un I...Service por servicio
│   ├── Services/           Reglas de negocio (RN-XX) de cada caso de uso
│   └── Helpers/            FechasNegocio: filtros de fecha en hora Argentina → UTC
├── DataAccess/             Datos
│   ├── Entities/           Cliente, Empleado, Admin, Beneficio, Producto, Movimiento,
│   │                       ReglaAcumulacion, Auditoria, CodigoRecuperacion (+ EntidadBase)
│   ├── Context/            FidelixDbContext (índices únicos, DeleteBehavior.Restrict)
│   ├── Repositories/       Un repositorio por entidad (+ Interfaces/)
│   ├── Migrations/         Migraciones de EF Core
│   └── Seed/               DbInitializer (crea la base y carga datos de prueba)
├── Shared/                 Compartido por todas las capas
│   ├── DTOs/               CreateDTO / UpdateDTO / ResponseDTO / FiltroDTO por módulo
│   ├── Exceptions/         Una excepción tipada por error de negocio
│   ├── Enums/              TipoMovimiento, CategoriaCliente, EstadoCanje, TipoReporte, ...
│   └── Security/           PasswordHasher (PBKDF2), PoliticaPassword, Roles
├── BusinessLogic.Tests/    Tests unitarios de services (xUnit + Moq)
├── API.IntegrationTests/   Tests de endpoints (WebApplicationFactory + SQLite en memoria)
└── bruno/                  Colección de requests de Bruno (ver src/bruno/README.md)
```

### Flujo de una petición

```
Cliente HTTP ──JSON──▶ Controller ──DTO──▶ Service ──Entidad──▶ Repository ──▶ DbContext ──▶ SQLite
                       (sintaxis,          (reglas de           (solo CRUD
                        try/catch → HTTP)   negocio, excepciones) con EF Core)
```

| Capa | Responsabilidad | No hace |
| --- | --- | --- |
| Controller | Recibe el HTTP, valida la sintaxis del DTO (DataAnnotations → 400), llama al service y traduce excepciones a códigos HTTP. | Reglas de negocio, acceso a datos. |
| Service | Reglas de negocio, traduce DTO ↔ Entidad, lanza excepciones tipadas. | Conocer HTTP o EF Core. |
| Repository | Operaciones de datos con EF Core (`AsNoTracking` en lecturas; el `Id` entero lo genera la base al insertar). | Reglas de negocio. |

Cada capa depende de **interfaces** de la siguiente, registradas en
`Program.cs` con `AddScoped<Interfaz, Implementacion>()`.

## Cómo ejecutar

```bash
cd src
dotnet run --project API/FidelixAPI.API.csproj
```

Al arrancar se crea `fidelix.db` (SQLite) y se cargan datos de prueba (el seed
solo corre sobre una base vacía: para empezar de cero, borrar `src/API/fidelix.db`
con la API apagada). En Development, la documentación interactiva queda en
`http://localhost:5106/scalar/v1`.

```bash
dotnet test     # desde src/: tests unitarios + de integración
```

### Datos de prueba (DbInitializer)

| Rol | Email | Contraseña | Datos |
| --- | --- | --- | --- |
| Cliente demo | `cliente.demo@fidelix.local` | `Cliente123!` | Id `1`, DNI `40333444`, 150 puntos vigentes y un lote de 80 ya vencido |
| Cliente Gold | `cliente.gold@fidelix.local` | `Gold1234!` | Id `3`, DNI `40111222`, acumula x1.5 (N1) |
| Cliente inactivo | `cliente.inactivo@fidelix.local` | `Inactivo123!` | Dado de baja (RN-14) |
| Admin | `admin@fidelix.local` | `Admin123!` | Id `1` |
| Empleado | `empleado@fidelix.local` | `Empleado123!` | Id `1` |
| Empleado inactivo | `empleado.inactivo@fidelix.local` | `Inactivo123!` | Dado de baja (RN-15) |

Cada tabla numera por separado: el cliente 1, el admin 1 y el empleado 1 son
personas distintas (el token y la auditoría los distinguen por el rol).

Además: beneficios "Descuento 10%" (100 pts) y "Producto Gratis Demo" (300 pts),
y una regla de acumulación vigente (0,1 puntos por $1, puntos válidos 180 días).

## Endpoints

Todos los endpoints, salvo los de `Auth` y el registro, exigen el token JWT
del login del rol indicado (`Authorization: Bearer ...`).

| Método | Ruta | Rol | CU | Respuestas |
| --- | --- | --- | --- | --- |
| **Auth** | | | | |
| POST | `/api/auth/login` | Público | CU-02 | 200, 401, 403 |
| POST | `/api/auth/empleados/login` | Público | CU-09 | 200, 401, 403 |
| POST | `/api/auth/admin/login` | Público | CU-14 | 200, 401 |
| POST | `/api/auth/recuperar-contrasena` | Público | CU-25 | 200 |
| POST | `/api/auth/recuperar-contrasena/confirmar` | Público | CU-25 | 200, 400 |
| **Cliente** | | | | |
| POST | `/api/clientes/registro` | Público | CU-01 | 201, 400, 409 |
| GET | `/api/clientes/me` | Cliente | CU-03 | 200, 404 |
| PUT | `/api/clientes/me` | Cliente | CU-03 | 200, 400, 404, 409 |
| DELETE | `/api/clientes/me` | Cliente | CU-03 | 204, 404 |
| GET | `/api/clientes/me/saldo` | Cliente | CU-04 | 200, 404, 500 |
| GET | `/api/clientes/me/movimientos?fechaDesde&fechaHasta&tipoMovimiento` | Cliente | CU-05 | 200 |
| GET | `/api/beneficios` | Cliente, Empleado | CU-06 | 200 |
| POST | `/api/canjes` | Cliente | CU-07, CU-23 | 201, 404, 409, 500 |
| GET | `/api/clientes/me/canjes` | Cliente | CU-08 | 200 |
| **Empleado (punto de venta)** | | | | |
| GET | `/api/clientes?documento={dni}` | Empleado, Admin | Búsqueda previa a CU-10, CU-11 y CU-13 | 200, 400, 404 |
| GET | `/api/clientes/{id}/saldo` | Empleado, Admin | CU-04 | 200, 404, 500 |
| GET | `/api/productos` | Empleado | CU-10 (productos activos) | 200 |
| POST | `/api/movimientos/acumulaciones/{clienteId}` | Empleado | CU-10 | 201, 400, 404, 409, 500 |
| POST | `/api/canjes/presencial/{clienteId}` | Empleado | CU-11, CU-23 | 201, 404, 409, 500 |
| PATCH | `/api/canjes/{id}/entregar` | Empleado | CU-11 (RN-10) | 200, 404, 409 |
| POST | `/api/clientes` | Empleado | CU-12 | 201, 400, 409 |
| GET | `/api/clientes/{id}/canjes` | Empleado | CU-13 | 200, 404 |
| **Admin** | | | | |
| GET | `/api/admin/clientes` | Admin | CU-15 | 200 |
| POST | `/api/admin/clientes` | Admin | CU-15 | 201, 400, 409 |
| PUT | `/api/admin/clientes/{id}` | Admin | CU-15 | 200, 400, 404, 409 |
| DELETE | `/api/admin/clientes/{id}` | Admin | CU-15 | 204, 404 |
| GET | `/api/admin/empleados` | Admin | CU-16 | 200 |
| POST | `/api/admin/empleados` | Admin | CU-16 | 201, 400, 409 |
| PUT | `/api/admin/empleados/{id}` | Admin | CU-16 | 200, 400, 404, 409 |
| DELETE | `/api/admin/empleados/{id}` | Admin | CU-16 | 204, 404 |
| GET | `/api/admin/beneficios` | Admin | CU-17 | 200 |
| GET | `/api/admin/beneficios/categorias` | Admin | CU-17 | 200 |
| POST | `/api/admin/beneficios` | Admin | CU-17 | 201, 400, 409 |
| PUT | `/api/admin/beneficios/{id}` | Admin | CU-17 | 200, 400, 404, 409 |
| PATCH | `/api/admin/beneficios/{id}/desactivar` | Admin | CU-17 | 200, 404 |
| GET | `/api/admin/productos` | Admin | CU-18 | 200 |
| POST | `/api/admin/productos` | Admin | CU-18 | 201, 400, 409 |
| PUT | `/api/admin/productos/{id}` | Admin | CU-18 | 200, 400, 404, 409 |
| PATCH | `/api/admin/productos/{id}/desactivar` | Admin | CU-18 | 200, 404 |
| GET | `/api/admin/reglas-acumulacion` | Admin | CU-19 | 200 |
| GET | `/api/admin/reglas-acumulacion/{id}` | Admin | CU-19 | 200, 404 |
| POST | `/api/admin/reglas-acumulacion` | Admin | CU-19 | 201, 400 |
| PUT | `/api/admin/reglas-acumulacion/{id}` | Admin | CU-19 | 200, 400, 404, 409 |
| GET | `/api/admin/movimientos?clienteId&tipoMovimiento&fechaDesde&fechaHasta` | Admin | CU-20 | 200 |
| GET | `/api/admin/auditoria?fechaDesde&fechaHasta&tipoOperacion&actorId` | Admin | CU-21 | 200 |
| GET | `/api/admin/auditoria/{id}` | Admin | CU-21 | 200, 404 |
| POST | `/api/admin/procesos/vencimiento-puntos` | Admin | CU-22 (ejecución manual) | 200 |
| POST | `/api/admin/procesos/bono-cumpleanos` | Admin | CU-26 (ejecución manual) | 200 |
| GET | `/api/admin/reportes?tipo&periodoDesde&periodoHasta&formato` | Admin | CU-24 (JSON o CSV) | 200 |
| GET | `/api/admin/reportes/comparacion?indicador&periodo1Desde&periodo1Hasta&periodo2Desde&periodo2Hasta` | Admin | CU-24, CU-20 (comparación) | 200 |

Respuestas comunes a todos los endpoints, que no se repiten en la tabla:

* **401** sin token o con token inválido; **403** con el token de otro rol (RN-11).
* **400** con datos mal formados (JSON inválido, campo obligatorio faltante, filtro con formato incorrecto): lo responde ASP.NET antes de llegar al controller, con el formato estándar `ProblemDetails` (`{ "title", "status", "errors" }`).
* **404** si un id de la ruta no es un número entero: la restricción `{id:int}` hace que la ruta no coincida.

Los procesos de CU-22 y CU-26 también corren solos una vez por día
(`ProcesoDiarioJob`); los endpoints de `procesos` permiten ejecutarlos a mano.

### Convenciones

* **El id (entero autoincremental) es la clave, no el DNI.** Las operaciones sobre un recurso
  reciben su id como parámetro de ruta, nunca en el body. El DNI solo se usa
  para buscar al cliente (`GET /api/clientes?documento=`), que devuelve su id.
* **Filtros** de las consultas como query params, recibidos en un único DTO con `[FromQuery]`.
* **Errores de negocio** (400, 404, 409, 500): `ErrorResponseDTO` → `{ "mensaje": "..." }`.
* **Listados**: `ListadoResponseDTO` → `{ "mensaje", "items": [...] }`; con la lista vacía, `mensaje` explica por qué.
* **Fechas de los filtros** en hora Argentina; si `fechaHasta` no trae hora, se incluye el día completo.

### Ejemplos

**Registrarse** — `POST /api/clientes/registro`

```json
{
  "nombre": "Ana Pérez",
  "documento": "40555666",
  "email": "ana@correo.com",
  "password": "Password123!",
  "fechaNacimiento": "1998-03-10T00:00:00Z"
}
```

`201 Created` (resumido):

```json
{
  "id": "…",
  "nombre": "Ana Pérez",
  "documento": "40555666",
  "email": "ana@correo.com",
  "telefono": null,
  "fechaNacimiento": "1998-03-10T00:00:00Z",
  "activo": true,
  "fechaRegistro": "…",
  "categoria": 1
}
```

`409 Conflict`: `{ "mensaje": "Ya existe un cliente registrado con ese documento." }`

**Consultar mi saldo** — `GET /api/clientes/me/saldo` (token de cliente; el id sale del token)

`200 OK`: `{ "puntosDisponibles": 150 }`

**Registrar una compra en el punto de venta** (token de empleado), en dos pasos:

1. Buscar al cliente por DNI — `GET /api/clientes?documento=40333444`

   `200 OK`: `{ "id": 1, "nombre": "Cliente Demo", "documento": "40333444", "categoria": "Estandar" }`

2. Registrar la compra con ese id en la ruta — `POST /api/movimientos/acumulaciones/1`

   ```json
   { "items": [ { "productoId": 1, "cantidad": 2 } ] }
   ```

   `201 Created` con el monto calculado, los puntos acreditados (pendientes 15
   días, N2) y el saldo disponible.

**Crear beneficio** — `POST /api/admin/beneficios` (token de admin)

```json
{ "nombre": "Café gratis", "descripcion": "Un café de regalo", "costoPuntos": 50, "categoria": 2 }
```

`201 Created` con el beneficio; `400` si `costoPuntos` no está entre 1 y
999.999 o si la categoría no existe; `409` si el nombre ya existe. Las categorías
válidas (1 = Descuento, 2 = Producto gratis, 3 = Experiencia, 4 = Otro) se consultan
en `GET /api/admin/beneficios/categorias`.

**Modificar beneficio** — `PUT /api/admin/beneficios/{id}`: solo cambian
los campos enviados (`{ "costoPuntos": 80 }`).

**Desactivar beneficio** — `PATCH /api/admin/beneficios/{id}/desactivar`:
baja lógica, devuelve el beneficio con `"activo": false`.

**Listar beneficios (admin)** — `GET /api/admin/beneficios`: todos los beneficios,
activos e inactivos, ordenados por nombre, con su `id` (el catálogo del cliente,
`GET /api/beneficios`, solo muestra los activos).
