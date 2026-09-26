# Fidelix API

API RESTful del sistema de fidelización de puntos **Fidelix**, hecha en
.NET 10 con arquitectura en capas (N-Tier) sobre SQLite, siguiendo el prompt
de arquitectura de la cátedra.

Esta versión contiene la **estructura base** y tres casos de uso modelo, uno
de cada tipo:

| CU | Tipo | Qué hace |
| --- | --- | --- |
| CU-01 Registrarse | Alta con reglas de negocio | Un cliente crea su cuenta (documento/email únicos, contraseña hasheada). |
| CU-04 Consultar saldo | Consulta calculada | Suma los puntos de los lotes vigentes del cliente (los vencidos no cuentan). |
| CU-17 ABM de beneficio | ABM con baja lógica | El admin crea, modifica y desactiva beneficios canjeables. |

El resto de los casos de uso (`../Docs/Use cases/`) se implementa sobre esta
base siguiendo los mismos patrones.

## Arquitectura

```
src/
├── API/                    Presentación: controllers, Program.cs (DI)
│   └── Controllers/        ClientesController, AdminBeneficiosController
├── BusinessLogic/          Negocio
│   ├── Interfaces/         IClienteService, IBeneficioService
│   └── Services/           ClienteService, BeneficioService
├── DataAccess/             Datos
│   ├── Entities/           Cliente, Beneficio, Movimiento (+ EntidadBase)
│   ├── Context/            FidelixDbContext
│   ├── Repositories/       Cliente/Beneficio/MovimientoRepository (+ Interfaces/)
│   ├── Migrations/         Migraciones de EF Core
│   └── Seed/               DbInitializer (crea la base y carga datos de prueba)
├── Shared/                 Compartido por todas las capas
│   ├── DTOs/               CreateDTO / UpdateDTO / ResponseDTO por entidad
│   ├── Exceptions/         Una excepción tipada por error de negocio
│   ├── Enums/              TipoMovimiento, CategoriaBeneficio
│   └── Security/           PasswordHasher (PBKDF2)
├── BusinessLogic.Tests/    Tests unitarios de services (xUnit + Moq)
├── API.IntegrationTests/   Tests de endpoints (WebApplicationFactory + SQLite en memoria)
└── bruno/                  Colección de requests de Bruno
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
| Repository | Operaciones de datos con EF Core (`AsNoTracking` en lecturas, `Guid` asignado al crear). | Reglas de negocio. |

Cada capa depende de **interfaces** de la siguiente, registradas en
`Program.cs` con `AddScoped<Interfaz, Implementacion>()`.

## Cómo ejecutar

```bash
cd src
dotnet run --project API/FidelixAPI.API.csproj
```

Al arrancar se crea `fidelix.db` (SQLite) y se cargan datos de prueba. En
Development, la documentación interactiva queda en `http://localhost:5299/scalar/v1`.

```bash
dotnet test     # tests unitarios + de integración
```

### Datos de prueba (DbInitializer)

- Cliente demo: Id `11111111-1111-1111-1111-111111111111`, documento
  `40333444`, email `cliente.demo@fidelix.local`, con 150 puntos vigentes y
  un lote de 80 ya vencido.
- Beneficios: "Descuento 10%" (100 pts) y "Producto Gratis Demo" (300 pts).

## Endpoints

| Método | Ruta | CU | Respuestas |
| --- | --- | --- | --- |
| POST | `/api/clientes/registro` | CU-01 | 201, 400, 409 |
| GET | `/api/clientes/{id}/saldo` | CU-04 | 200, 404, 500 |
| POST | `/api/admin/beneficios` | CU-17 | 201, 400, 409 |
| PUT | `/api/admin/beneficios/{id}` | CU-17 | 200, 400, 404, 409 |
| PATCH | `/api/admin/beneficios/{id}/desactivar` | CU-17 | 200, 404 |

> Todavía no hay autenticación: se agrega con el login (CU-02/09/14). En ese
> momento el saldo pasa a `GET /api/clientes/me/saldo` y el ABM de beneficios
> queda restringido al rol Admin.

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

`201 Created`:

```json
{
  "id": "…",
  "nombre": "Ana Pérez",
  "documento": "40555666",
  "email": "ana@correo.com",
  "telefono": null,
  "fechaNacimiento": "1998-03-10T00:00:00Z",
  "activo": true,
  "fechaRegistro": "…"
}
```

`409 Conflict`: `{ "message": "Ya existe un cliente registrado con ese documento." }`

**Consultar saldo** — `GET /api/clientes/11111111-1111-1111-1111-111111111111/saldo`

`200 OK`: `{ "puntosDisponibles": 150 }`

**Crear beneficio** — `POST /api/admin/beneficios`

```json
{ "nombre": "Café gratis", "descripcion": "Un café de regalo", "costoPuntos": 50, "categoria": 1 }
```

`201 Created` con el beneficio; `400` si `costoPuntos <= 0`; `409` si el
nombre ya existe.

**Modificar beneficio** — `PUT /api/admin/beneficios/{id}`: solo cambian
los campos enviados (`{ "costoPuntos": 80 }`).

**Desactivar beneficio** — `PATCH /api/admin/beneficios/{id}/desactivar`:
baja lógica, devuelve el beneficio con `"activo": false`.
