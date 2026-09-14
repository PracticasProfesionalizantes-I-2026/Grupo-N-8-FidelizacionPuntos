# Fidelix API

API RESTful del sistema de fidelización de puntos Fidelix, construida en
**.NET 10 / ASP.NET Core Web API** con arquitectura en capas (N-Tier),
siguiendo la adenda de arquitectura de la cátedra. Implementa los 26 casos
de uso documentados en [`../Docs/Use cases/`](../Docs/Use%20cases/) y las
reglas de negocio de [`../Docs/Business/Documentacion-Fidelix.md`](../Docs/Business/Documentacion-Fidelix.md).

## Arquitectura

```
src/
├── API/                    → Presentación: controllers, Program.cs, JWT, Scalar
├── BusinessLogic/          → Servicios (reglas de negocio) e interfaces
│   └── BusinessLogic.Tests → xUnit + Moq (sin tocar base de datos)
├── DataAccess/             → Entidades, DbContext, repositorios, migraciones
│   └── Seed/DbInitializer  → Crea la base y carga datos de prueba al arrancar
├── Shared/                 → DTOs, excepciones tipadas, enums, configuración
├── API.IntegrationTests/   → WebApplicationFactory (pipeline HTTP real)
└── bruno/                  → Colección de requests (ver bruno/README.md)
```

**Flujo obligatorio:** `Controller → Service → Repository → DbContext`. Los
controllers nunca acceden a datos directamente; los services nunca escriben
LINQ-to-entities (eso vive en los repositorios). Cada capa depende de la
interfaz de la capa inferior, inyectada por constructor (ver
`API/Program.cs` para el registro de dependencias).

### Diagrama de capas

```
┌─────────────────────────────────────────────────────────┐
│ API (Presentación)                                       │
│  13 controllers · JWT Bearer · validación de DTOs        │
│  try/catch explícito por acción → mapea excepciones a    │
│  400/401/403/404/409/500                                 │
└───────────────────────────┬───────────────────────────────┘
                             │ interfaces (I...Service)
┌───────────────────────────▼───────────────────────────────┐
│ BusinessLogic (Servicios)                                  │
│  12 servicios: AuthService, ClienteService,                │
│  ClienteAdminService, EmpleadoAdminService, ProductoService,│
│  BeneficioService, ReglaAcumulacionService, PuntosService,  │
│  MovimientoService, CanjeService, AuditoriaService,         │
│  ReporteService                                             │
└───────────────────────────┬───────────────────────────────┘
                             │ interfaces (I...Repository)
┌───────────────────────────▼───────────────────────────────┐
│ DataAccess (Repositorios + EF Core)                        │
│  9 entidades · FidelixDbContext · SQLite                   │
│  DeleteBehavior.Restrict · AsNoTracking en lecturas         │
└─────────────────────────────────────────────────────────┘
```

## Entidades

`Cliente`, `Empleado`, `Admin` (las tres heredan `CuentaConCredenciales`:
email, password, activo, bloqueo por intentos fallidos), `Producto`,
`Beneficio`, `ReglaAcumulacion`, `Movimiento` (ledger único de puntos:
acumulación/canje/bono/vencimiento), `CodigoRecuperacion`, `Auditoria`.

## Cómo correr

```bash
dotnet build                                    # compila toda la solución
dotnet test                                     # 53 tests unitarios + 15 de integración
dotnet run --project API/FidelixAPI.API.csproj  # levanta la API
```

Con `ASPNETCORE_ENVIRONMENT=Development`, la documentación interactiva
queda en `/scalar/v1`. Al arrancar, la API aplica las migraciones
pendientes y —si la base está vacía— siembra datos de prueba
(`DbInitializer`):

| Usuario | Email | Password |
| --- | --- | --- |
| Cliente | `cliente.demo@fidelix.local` | `Cliente123!` |
| Empleado | `empleado.demo@fidelix.local` | `Empleado123!` |
| Admin | `admin@fidelix.local` | `Admin123!` |

## Autenticación

Un único endpoint de login (`POST /api/auth/login`) para los tres roles:
`AuthService` prueba las credenciales en cascada Cliente → Empleado →
Admin y emite un JWT con el rol correspondiente. Los controllers protegen
sus acciones con `[Authorize(Roles = "...")]`.

## Catálogo de endpoints

| Método | Ruta | Rol | CU |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | público | CU-02/09/14 |
| POST | `/api/auth/recuperar-contrasena` | público | CU-25 |
| POST | `/api/auth/recuperar-contrasena/confirmar` | público | CU-25 |
| POST | `/api/clientes/registro` | público | CU-01 |
| POST | `/api/clientes` | Empleado | CU-12 |
| GET/PUT/DELETE | `/api/clientes/me` | Cliente | CU-03 |
| GET | `/api/clientes/me/saldo` | Cliente | CU-04 |
| GET | `/api/clientes/me/movimientos` | Cliente | CU-05 |
| GET | `/api/clientes/me/canjes` | Cliente | CU-08 |
| GET | `/api/clientes/{documento}/canjes` | Empleado | CU-13 |
| GET | `/api/beneficios` | Cliente/Empleado | CU-06 |
| POST | `/api/canjes` | Cliente/Empleado | CU-07/11 |
| POST | `/api/movimientos/acumulaciones` | Empleado | CU-10 |
| POST/PUT/DELETE | `/api/admin/clientes` | Admin | CU-15 |
| POST/PUT/DELETE | `/api/admin/empleados` | Admin | CU-16 |
| POST/PUT/PATCH | `/api/admin/beneficios` | Admin | CU-17 |
| POST/PUT/PATCH | `/api/admin/productos` | Admin | CU-18 |
| GET/GET{id}/POST/PUT | `/api/admin/reglas-acumulacion` | Admin | CU-19 |
| GET | `/api/admin/movimientos` | Admin | CU-20 |
| GET/GET{id} | `/api/admin/auditoria` | Admin | CU-21 |
| GET | `/api/admin/reportes` | Admin | CU-24 |

Sin endpoint HTTP (procesos batch de Sistema): `AplicarVencimientoAsync`
(CU-22) y `AplicarBonoCumpleanosAsync` (CU-26), en `IPuntosService`.
Corren solos, programados como `BackgroundService` (`BusinessLogic/Jobs/`):
una vez al arrancar la API y luego cada 24hs.

### Ejemplo: registrar una compra y consultar el saldo

```bash
# Login como empleado
curl -X POST http://localhost:5299/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"empleado.demo@fidelix.local","password":"Empleado123!"}'
# → { "token": "...", "rol": 1, "expiraEn": "..." }

# Registrar una compra de 2000 (con el token del paso anterior)
curl -X POST http://localhost:5299/api/movimientos/acumulaciones \
  -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -d '{"clienteDocumento":"40333444","items":[{"producto":"Producto Demo A","cantidad":1,"monto":2000}]}'
# → 201 Created, { "puntos": 2000, ... }
```

Ver la colección completa (con casos de éxito y de error para cada
endpoint) en [`bruno/`](bruno/).

## Documentación relacionada

- [`../Docs/Business/Plan-Tecnico-API-Fase1.md`](../Docs/Business/Plan-Tecnico-API-Fase1.md) — plan técnico original y decisiones de diseño.
- [`../Docs/Use cases/`](../Docs/Use%20cases/) — especificación de los 26 casos de uso.
- [`AGENTS.md`](AGENTS.md) — contexto operativo para agentes de IA.
